using System;
using System.Text;
using System.Threading.Tasks;
using Offichat.Application.Session;
using Offichat.Network;
using Offichat.Application.PacketRouting;

namespace Offichat.Application.Handlers
{
    public class MoveHandler : IPacketHandler
    {
        public ushort PacketId => 2; // Örnek: Move Packet

        public async Task HandleAsync(PacketBase packet, PlayerSession session)
        {
            // Payload: "x;y;z" gibi bir string (2D veya 3D koordinatlar)
            var payloadStr = packet.GetPayloadAsString();
            var parts = payloadStr.Split(';');
            if (parts.Length >= 2)
            {
                float x = float.Parse(parts[0]);
                float y = float.Parse(parts[1]);
                float z = parts.Length >= 3 ? float.Parse(parts[2]) : 0;

                Console.WriteLine($"[MoveHandler] Session {session.SessionId} moved to ({x},{y},{z})");

                // Örnek: client’a konum onayı gönder
                var responsePayload = $"ACK:{x};{y};{z}";
                var response = new UDPPacket(
                    packet.PacketId,
                    session.SessionId,
                    Encoding.UTF8.GetBytes(responsePayload),
                    sequenceNumber: 0,
                    timestamp: (uint)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                );

                if (session.UdpEndpoint != null)
                    await session.SendUdpAsync(response);
            }
        }
    }
}
