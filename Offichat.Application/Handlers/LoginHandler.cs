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
        public ushort PacketId => 1;

        public async Task HandleAsync(PacketBase packet, PlayerSession session)
        {
            string username = packet.GetPayloadAsString();

            if (string.IsNullOrWhiteSpace(username))
            {
                Console.WriteLine($"[HANDLER] Login failed for Session {session.SessionId}");
                var failResponse = new TCPPacket((byte)PacketId, session.SessionId, Encoding.UTF8.GetBytes("Login Failed"));
                await session.SendTcpAsync(failResponse);
                return;
            }

            session.Username = username;
            session.UpdateActivity();

            Console.WriteLine($"[HANDLER] Session {session.SessionId} logged in as {username}");

            var response = new TCPPacket((byte)PacketId, session.SessionId, Encoding.UTF8.GetBytes("Login OK"));
            await session.SendTcpAsync(response);
        }
    }
}
