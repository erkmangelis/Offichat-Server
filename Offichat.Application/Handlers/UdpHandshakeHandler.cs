using Offichat.Application.PacketRouting;
using Offichat.Application.Session;
using Offichat.Network;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Offichat.Application.Handlers
{
    public class UdpHandshakeHandler : IPacketHandler
    {
        public ushort PacketId => 100;

        public async Task HandleAsync(PacketBase packet, PlayerSession session)
        {
            if (string.IsNullOrEmpty(session.Username))
            {
                Console.WriteLine($"[HANDLER] UDP Handshake ignored for Session {session.SessionId} (not logged in)");
                return;
            }

            Console.WriteLine($"[HANDLER] UDP Handshake received for Session {session.SessionId}");

            var response = new UDPPacket((byte)PacketId, session.SessionId, Encoding.UTF8.GetBytes("UDP Handshake OK"));
            await session.SendUdpAsync(response);
        }
    }
}
