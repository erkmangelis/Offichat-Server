using Offichat.Application.Exceptions;
using Offichat.Network;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Offichat.Application.Session
{
    public class SessionManager
    {
        private readonly ConcurrentDictionary<uint, PlayerSession> _sessions = new();
        private readonly ConcurrentDictionary<IPEndPoint, uint> _udpSessionMap = new(); // UDP paket -> SessionId
        private uint _nextSessionId = 1; // Otomatik artan SessionId

        // Timeout ayarları
        private readonly TimeSpan AFK_TIMEOUT;
        private readonly TimeSpan SESSION_TIMEOUT;

        // CancellationToken, monitoring task'i için
        private readonly CancellationToken _cancellationToken;

        // Batch removal queue
        private readonly ConcurrentQueue<uint> _sessionsToRemove = new();

        public SessionManager(int afkTimeoutSeconds, int sessionTimeoutSeconds, CancellationToken cancellationToken)
        {
            AFK_TIMEOUT = TimeSpan.FromSeconds(afkTimeoutSeconds);
            SESSION_TIMEOUT = TimeSpan.FromSeconds(sessionTimeoutSeconds);
            _cancellationToken = cancellationToken;

            StartMonitoring();
        }

        // Yeni TCP client bağlandı, UdpClient referansı verilmeli
        public PlayerSession CreateSession(TcpClient tcpClient, UdpClient udpClient)
        {
            uint sessionId = _nextSessionId++;
            var session = new PlayerSession(sessionId, tcpClient, udpClient);

            if (!_sessions.TryAdd(sessionId, session))
                throw new SessionException($"Failed to add new session with id {sessionId}");

            return session;
        }

        // UDP paket geldiğinde session bul
        public PlayerSession? GetSessionByUdp(IPEndPoint endpoint)
        {
            if (_udpSessionMap.TryGetValue(endpoint, out uint sessionId))
                return GetSession(sessionId);

            return null;
        }

        // UDP endpoint -> sessionId eşlemesi ekle
        public void BindUdp(PlayerSession session, IPEndPoint endpoint)
        {
            session.UdpEndpoint = endpoint;
            _udpSessionMap[endpoint] = session.SessionId;
        }

        // Session silme
        public void RemoveSession(uint sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                if (session.UdpEndpoint != null)
                    _udpSessionMap.TryRemove(session.UdpEndpoint, out _);

                session.Close();
            }
        }

        // Session sorgulama
        public PlayerSession? GetSession(uint sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        // PlayerId ile session sorgulama
        public PlayerSession? GetSessionByPlayerId(int playerId)
        {
            // PlayerId'si herhangi bir session'da eşleşiyorsa döndür
            return _sessions.Values.FirstOrDefault(s => s.PlayerId == playerId);
        }

        // Tüm session’ları listeleme (debug)
        public PlayerSession[] GetAllSessions() => _sessions.Values.ToArray();

        // Tüm aktif oyunculara TCP paketi gönder
        public async Task BroadcastTcpAsync(PacketBase packet)
        {
            foreach (var session in _sessions.Values)
            {
                // Sadece giriş yapmış (Login olmuş) kullanıcılara gönder
                if (session.TcpClient.Connected && !string.IsNullOrEmpty(session.Username))
                {
                    await session.SendTcpAsync((TCPPacket)packet);
                }
            }
        }

        // Belirli bir oyuncu HARİÇ diğerlerine gönder (Örn: "Ben girdim" paketi bana gelmesin)
        public async Task BroadcastTcpExceptAsync(PacketBase packet, uint exceptSessionId)
        {
            foreach (var session in _sessions.Values)
            {
                if (session.SessionId != exceptSessionId && session.TcpClient.Connected && !string.IsNullOrEmpty(session.Username))
                {
                    await session.SendTcpAsync((TCPPacket)packet);
                }
            }
        }

        // UDP üzerinden herkese yayın yap (Hareket verisi için)
        public async Task BroadcastUdpAsync(UDPPacket packet, uint exceptSessionId = 0)
        {
            foreach (var session in _sessions.Values)
            {
                // 1. Kendime geri gönderme (Client Prediction varsa gerek yok)
                if (session.SessionId == exceptSessionId) continue;

                // 2. Sadece UDP bağlantısı kurmuş (Handshake yapmış) ve Login olmuş oyunculara
                if (session.UdpEndpoint != null && !string.IsNullOrEmpty(session.Username))
                {
                    await session.SendUdpAsync(packet);
                }
            }
        }

        // AFK / Timeout kontrolü
        public void StartMonitoring()
        {
            Task.Run(async () =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    foreach (var session in _sessions.Values)
                    {
                        var idleTime = DateTime.UtcNow - session.LastActivity;

                        if (idleTime > SESSION_TIMEOUT)
                        {
                            // Batch removal kuyruğuna ekle
                            _sessionsToRemove.Enqueue(session.SessionId);
                        }
                        else if (idleTime > AFK_TIMEOUT && !session.IsAfk)
                        {
                            session.SetAfk(true);
                            Console.WriteLine($"[Session] Session {session.SessionId} is now AFK.");
                        }
                        else if (idleTime <= AFK_TIMEOUT && session.IsAfk)
                        {
                            session.SetAfk(false);
                            Console.WriteLine($"[Session] Session {session.SessionId} is no longer AFK.");
                        }
                    }

                    // Batch removal
                    while (_sessionsToRemove.TryDequeue(out var sessionId))
                    {
                        RemoveSession(sessionId);
                        Console.WriteLine($"[Session] Removed Session {sessionId} due to timeout.");
                    }

                    await Task.Delay(5000, _cancellationToken);
                }
            }, _cancellationToken);
        }
    }
}
