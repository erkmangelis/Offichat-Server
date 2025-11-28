using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Offichat.Network;

namespace Offichat.Application.Session
{
    public class PlayerSession
    {
        public uint SessionId { get; private set; }
        public TcpClient TcpClient { get; private set; }
        public UdpClient UdpClient { get; private set; }

        public IPEndPoint? UdpEndpoint { get; set; }
        public string? Username { get; set; }
        public int? UserId { get; set; }
        public int? PlayerId { get; set; }

        public DateTime ConnectedAt { get; private set; }
        public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
        public bool IsAfk { get; private set; } = false;

        public PlayerSession(uint sessionId, TcpClient tcpClient, UdpClient udpClient)
        {
            SessionId = sessionId;
            TcpClient = tcpClient;
            UdpClient = udpClient;

            ConnectedAt = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
        }

        public void UpdateActivity()
        {
            LastActivity = DateTime.UtcNow;
            IsAfk = false;
        }

        public void SetAfk(bool afk)
        {
            IsAfk = afk;
        }

        // TCP üzerinden paket gönder
        public async Task SendTcpAsync(TCPPacket packet)
        {
            if (TcpClient.Connected)
            {
                var stream = TcpClient.GetStream();
                byte[] data = packet.ToBytes();
                await stream.WriteAsync(data, 0, data.Length);
            }
        }

        // UDP üzerinden paket gönder
        public async Task SendUdpAsync(UDPPacket packet)
        {
            if (UdpEndpoint != null)
            {
                byte[] data = packet.ToBytes();
                await UdpClient.SendAsync(data, data.Length, UdpEndpoint);
            }
        }

        public void Close()
        {
            try
            {
                TcpClient.Close();
            }
            catch { }
        }

        public override string ToString()
        {
            string tcpInfo = TcpClient?.Client?.RemoteEndPoint?.ToString() ?? "Disconnected";
            string udpInfo = UdpEndpoint?.ToString() ?? "None";
            return $"Session {SessionId} (User: {Username ?? "Guest"}, TCP: {tcpInfo}, UDP: {udpInfo})";
        }
    }
}
