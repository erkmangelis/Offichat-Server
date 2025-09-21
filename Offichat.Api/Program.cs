using Offichat.Application;
using Offichat.Application.Handlers;
using Offichat.Application.PacketRouting;
using Offichat.Application.Session;
using Offichat.Network;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Offichat.Api
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("[Offichat] Server starting...");

            // Konfigürasyon yükle
            var config = ServerConfig.Load("appsettings.json");

            // İptal tokeni ve Ctrl+C yakalama
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var sessionManager = new SessionManager(config.AfkTimeoutSeconds, config.SessionTimeoutSeconds, cts.Token);
            var packetRouter = new PacketRouter();

            // IPacketHandler implementasyonlarını otomatik register et
            var handlerType = typeof(IPacketHandler);
            var handlers = handlerType.Assembly
                                      .GetTypes()
                                      .Where(t => handlerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in handlers)
            {
                try
                {
                    var instance = (IPacketHandler)Activator.CreateInstance(type)!;
                    packetRouter.RegisterHandler(instance);
                    Console.WriteLine($"[Router] Registered handler: {type.FullName} for PacketId={instance.PacketId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Router] Failed to register {type.FullName}: {ex.Message}");
                }
            }

            var tcpManager = new TCPListenerManager(config.IP, config.TCPPort);
            var udpManager = new UDPListenerManager(config.IP, config.UDPPort);

            // TCP events
            tcpManager.OnClientConnected += client =>
            {
                var session = sessionManager.CreateSession(client, udpManager.UdpClient);
                Console.WriteLine($"[TCP] Client connected: {session}");
            };

            tcpManager.OnClientDisconnected += client =>
            {
                var session = sessionManager.GetAllSessions()
                    .FirstOrDefault(s => s.TcpClient == client);

                if (session != null)
                {
                    sessionManager.RemoveSession(session.SessionId);
                    Console.WriteLine($"[TCP] Client disconnected: {session}");
                }
            };

            tcpManager.OnPacketReceived += async (client, packet) =>
            {
                var session = sessionManager.GetAllSessions()
                    .FirstOrDefault(s => s.TcpClient == client);

                if (session != null)
                {
                    session.UpdateActivity();
                    await packetRouter.RouteAsync(packet, session);
                }
            };

            // UDP events
            udpManager.OnPacketReceived += async (remoteEndPoint, packet) =>
            {
                var session = sessionManager.GetSessionByUdp(remoteEndPoint);

                if (session == null)
                {
                    session = sessionManager.GetSession(packet.SessionId);
                    if (session != null)
                    {
                        if (string.IsNullOrEmpty(session.Username))
                        {
                            Console.WriteLine($"[UDP] Ignoring packet from {remoteEndPoint} (not logged in)");
                            return;
                        }

                        sessionManager.BindUdp(session, remoteEndPoint);
                        Console.WriteLine($"[UDP] Bound endpoint {remoteEndPoint} -> Session {session.SessionId}");
                    }
                    else
                    {
                        Console.WriteLine($"[UDP] Unknown session for {remoteEndPoint}, ignoring.");
                        return;
                    }
                }

                session.UpdateActivity();
                await packetRouter.RouteAsync(packet, session);
            };

            // Başlat
            tcpManager.Start(cts.Token);
            udpManager.Start(cts.Token);

            Console.WriteLine("[Offichat] Server started. Press Ctrl+C to stop...");

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException) { }

            // Stop
            tcpManager.Stop();
            udpManager.Stop();

            Console.WriteLine("[Offichat] Server stopped.");
        }
    }
}
