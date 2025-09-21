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

            // Config dosyasından ip ve portları oku
            var config = ServerConfig.Load("appsettings.json");

            var sessionManager = new SessionManager();
            var packetRouter = new PacketRouter();

            // Tüm IPacketHandler implementasyonlarını otomatik register et
            var handlerType = typeof(IPacketHandler);
            var handlers = Assembly.GetExecutingAssembly()
                                   .GetTypes()
                                   .Where(t => handlerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in handlers)
            {
                var instance = (IPacketHandler)Activator.CreateInstance(type)!;
                packetRouter.RegisterHandler(instance);
                Console.WriteLine($"[Router] Registered handler: {type.Name} for PacketId={instance.PacketId}");
            }

            var tcpManager = new TCPListenerManager(config.IP, config.TCPPort);
            var udpManager = new UDPListenerManager(config.IP, config.UDPPort);

            // CancellationTokenSource ile durdurma mekanizması
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true; // Uygulamanın hemen kapanmasını önle
                cts.Cancel();
            };

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
                        sessionManager.BindUdp(session, remoteEndPoint);
                        Console.WriteLine($"[UDP] Bound new endpoint {remoteEndPoint} to Session {session.SessionId}");
                    }
                    else
                    {
                        Console.WriteLine($"[UDP] Unknown session for endpoint {remoteEndPoint}, ignoring packet.");
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
