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
        public IPEndPoint? UdpEndpoint { get; set; }
        private readonly UdpClient _udpClient;

        public string? Username { get; set; }
        public DateTime ConnectedAt { get; private set; }
        public DateTime LastActivity { get; private set; }

        public PlayerSession(uint sessionId, TcpClient tcpClient, UdpClient udpClient)
        {
            SessionId = sessionId;
            TcpClient = tcpClient;
            _udpClient = udpClient;

            ConnectedAt = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
        }

        public void UpdateActivity()
        {
            LastActivity = DateTime.UtcNow;
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
                await _udpClient.SendAsync(data, data.Length, UdpEndpoint);
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
            return $"Session {SessionId} (User: {Username ?? "Guest"}, TCP: {TcpClient.Client.RemoteEndPoint}, UDP: {UdpEndpoint})";
        }
    }
}
