using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Offichat.Application;
using Offichat.Application.Handlers;
using Offichat.Domain.Interfaces;
using Offichat.Application.PacketRouting;
using Offichat.Application.Session;
using Offichat.Infrastructure.Persistence;
using Offichat.Infrastructure.Services;
using Offichat.Network;

namespace Offichat.Api
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("[Offichat] Server starting...");

            // 1. Konfigürasyonu Yükle
            var config = ServerConfig.Load("appsettings.json");

            // 2. Servis Konteynerini Hazırla (Dependency Injection)
            var services = new ServiceCollection();

            // -- Database Bağlantısı --
            services.AddDbContext<OffichatDbContext>(options =>
            {
                options.UseNpgsql(config.ConnectionString);
            }, ServiceLifetime.Scoped);
            // Not: Handler'lar her paket için yeniden çağrılmayıp memoryde tutuluyorsa Scoped yerine Transient gerekebilir, 
            // ama PacketRouter yapısına göre Singleton handler kullanıyorsak DbContext Factory kullanmalıyız.
            // *Basitlik için* şimdilik Handler'ları Transient (her istekte yeni) yapalım.

            // Şifreleme servisini ekle
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            // -- Core Servisler --
            services.AddSingleton(config);
            services.AddSingleton<PacketRouter>();

            // SessionManager Singleton olmalı (Tüm sunucu için tek)
            services.AddSingleton<SessionManager>(sp =>
                new SessionManager(config.AfkTimeoutSeconds, config.SessionTimeoutSeconds, sp.GetRequiredService<CancellationTokenSource>().Token));

            services.AddSingleton<CancellationTokenSource>();

            // -- Handler'ları Otomatik Bul ve Kaydet --
            var handlerType = typeof(IPacketHandler);
            var handlerImplementationTypes = handlerType.Assembly
                                      .GetTypes()
                                      .Where(t => handlerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in handlerImplementationTypes)
            {
                // Handler'ları Transient ekliyoruz ki her ihtiyaç duyulduğunda DbContext'i taze alabilsinler
                services.AddTransient(handlerType, type);
                // Ayrıca kendisi olarak da kaydedelim (Reflection ile bulurken kolaylık olsun diye)
                services.AddTransient(type);
            }

            // 3. Provider'ı İnşa Et
            var serviceProvider = services.BuildServiceProvider();

            // İptal tokeni (Ctrl+C için)
            var cts = serviceProvider.GetRequiredService<CancellationTokenSource>();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // 4. Handler'ları Router'a Kaydet
            var packetRouter = serviceProvider.GetRequiredService<PacketRouter>();
            foreach (var type in handlerImplementationTypes)
            {
                try
                {
                    // Artık Activator yerine serviceProvider kullanıyoruz!
                    // Böylece LoginHandler(OffichatDbContext db) constructor'ı çalışabilecek.
                    var handlerInstance = (IPacketHandler)serviceProvider.GetRequiredService(type);
                    packetRouter.RegisterHandler(handlerInstance);
                    Console.WriteLine($"[Router] Registered handler: {type.Name} for PacketId={handlerInstance.PacketId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Router] Failed to register {type.Name}: {ex.Message}");
                }
            }

            // 5. Network Listener'ları Başlat
            var tcpManager = new TCPListenerManager(config.IP, config.TCPPort);
            var udpManager = new UDPListenerManager(config.IP, config.UDPPort);
            var sessionManager = serviceProvider.GetRequiredService<SessionManager>();

            // TCP Events
            tcpManager.OnClientConnected += client =>
            {
                var session = sessionManager.CreateSession(client, udpManager.UdpClient);
                Console.WriteLine($"[TCP] Client connected: {session}");
            };

            tcpManager.OnClientDisconnected += async client =>
            {
                var session = sessionManager.GetAllSessions()
                    .FirstOrDefault(s => s.TcpClient == client);

                if (session != null)
                {
                    // 1. Session'ı listeden sil (Artık mesaj alamaz)
                    sessionManager.RemoveSession(session.SessionId);
                    Console.WriteLine($"[TCP] Client disconnected: {session.Username ?? "Guest"} (PlayerId: {session.PlayerId})");

                    // 2. Eğer oyunda olan (PlayerId'si olan) biri çıktıysa, diğerlerine haber ver
                    if (session.PlayerId.HasValue)
                    {
                        var despawnPayload = new Offichat.Application.DTOs.DespawnPayload
                        {
                            PlayerId = session.PlayerId.Value
                        };

                        string json = System.Text.Json.JsonSerializer.Serialize(despawnPayload);

                        // Packet ID: 6 (Despawn)
                        // TCPPacket namespace'ini eklemeyi unutmayın: using Offichat.Network;
                        var packet = new TCPPacket(6, session.SessionId, System.Text.Encoding.UTF8.GetBytes(json));

                        // Kalan herkese gönder
                        await sessionManager.BroadcastTcpAsync(packet);
                        Console.WriteLine($"[Server] Broadcasted Despawn for Player {session.PlayerId}");
                    }
                }
            };

            tcpManager.OnPacketReceived += async (client, packet) =>
            {
                var session = sessionManager.GetAllSessions().FirstOrDefault(s => s.TcpClient == client);
                if (session != null)
                {
                    session.UpdateActivity();
                    // Router'a gönder
                    await packetRouter.RouteAsync(packet, session);
                }
            };

            // UDP Events
            udpManager.OnPacketReceived += async (remoteEndPoint, packet) =>
            {
                var session = sessionManager.GetSessionByUdp(remoteEndPoint);
                if (session == null)
                {
                    session = sessionManager.GetSession(packet.SessionId);
                    if (session != null && !string.IsNullOrEmpty(session.Username)) // Login kontrolü Username yerine Player nesnesiyle yapılacak ilerde
                    {
                        sessionManager.BindUdp(session, remoteEndPoint);
                    }
                    else
                    {
                        return; // Tanımsız paket
                    }
                }
                session.UpdateActivity();
                await packetRouter.RouteAsync(packet, session);
            };

            // Start
            tcpManager.Start(cts.Token);
            udpManager.Start(cts.Token);

            Console.WriteLine("[Offichat] Server started. Press Ctrl+C to stop...");
            try { await Task.Delay(Timeout.Infinite, cts.Token); } catch (TaskCanceledException) { }

            tcpManager.Stop();
            udpManager.Stop();
            Console.WriteLine("[Offichat] Server stopped.");
        }
    }
}