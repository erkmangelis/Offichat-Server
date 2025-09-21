using Offichat.Application.Session;
using Offichat.Network;

namespace Offichat.Application.PacketRouting
{
    public interface IPacketHandler
    {
        ushort PacketId { get; }
        Task HandleAsync(PacketBase packet, PlayerSession session);
    }
}
