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

        // Yeni TCP client bağlandı, UdpClient referansı verilmeli
        public PlayerSession CreateSession(TcpClient tcpClient, UdpClient udpClient)
        {
            uint sessionId = _nextSessionId++;
            var session = new PlayerSession(sessionId, tcpClient, udpClient);

            if (!_sessions.TryAdd(sessionId, session))
                throw new Exception("Failed to add new session");

            return session;
        }

        // UDP paket geldiğinde session bul
        public PlayerSession? GetSessionByUdp(IPEndPoint endpoint)
        {
            if (_udpSessionMap.TryGetValue(endpoint, out uint sessionId))
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                    return session;
            }
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

        // Tüm session’ları listeleme (debug)
        public PlayerSession[] GetAllSessions() => _sessions.Values.ToArray();
    }
}
