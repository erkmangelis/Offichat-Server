using Offichat.Application.PacketRouting;
using Offichat.Application.Session;
using Offichat.Network;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Offichat.Application.Handlers
{
    public class LoginHandler : IPacketHandler
    {
        public ushort PacketId => 1; // Örnek: Login Request

        public async Task HandleAsync(PacketBase packet, PlayerSession session)
        {
            string username = packet.GetPayloadAsString();
            session.Username = username;
            session.UpdateActivity();

            Console.WriteLine($"[Handler] Session {session.SessionId} logged in as {username}");

            // TCP üzerinden async cevap gönder
            var response = new TCPPacket((byte)PacketId, session.SessionId, Encoding.UTF8.GetBytes("Login OK"));
            await session.SendTcpAsync(response);
        }
    }
}
