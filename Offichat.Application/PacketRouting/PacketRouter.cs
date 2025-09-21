using Offichat.Application.Session;
using Offichat.Network;

namespace Offichat.Application.PacketRouting
{
    public class PacketRouter
    {
        private readonly Dictionary<ushort, IPacketHandler> _handlers = new();

        public void RegisterHandler(IPacketHandler handler)
        {
            _handlers[handler.PacketId] = handler;
        }

        public async Task RouteAsync(PacketBase packet, PlayerSession session)
        {
            if (_handlers.TryGetValue(packet.PacketId, out var handler))
            {
                await handler.HandleAsync(packet, session);
            }
            else
            {
                Console.WriteLine($"[Router] No handler registered for PacketId={packet.PacketId}");
            }
        }
    }
}
