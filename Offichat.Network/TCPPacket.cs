using System;

namespace Offichat.Network
{
    public class TCPPacket : PacketBase
    {
        public ushort PacketLength { get; private set; }

        public TCPPacket(byte packetId, uint sessionId, byte[] payload, byte flags = 0)
            : base(packetId, sessionId, payload, flags)
        {
            PacketLength = (ushort)(1 + 1 + 4 + Payload.Length); // PacketId + Flags + SessionId + Payload
        }

        public override byte[] ToBytes()
        {
            byte[] buffer = new byte[2 + PacketLength]; // 2 byte PacketLength + rest
            buffer[0] = (byte)(PacketLength >> 8);
            buffer[1] = (byte)(PacketLength & 0xFF);
            buffer[2] = PacketId;
            buffer[3] = Flags;
            buffer[4] = (byte)((SessionId >> 24) & 0xFF);
            buffer[5] = (byte)((SessionId >> 16) & 0xFF);
            buffer[6] = (byte)((SessionId >> 8) & 0xFF);
            buffer[7] = (byte)(SessionId & 0xFF);
            Array.Copy(Payload, 0, buffer, 8, Payload.Length);
            return buffer;
        }

        public static TCPPacket FromBytes(byte[] data)
        {
            if (data.Length < 8) throw new ArgumentException("Data too short to be a valid TCP packet");

            ushort length = (ushort)((data[0] << 8) | data[1]);
            byte packetId = data[2];
            byte flags = data[3];
            uint sessionId = (uint)((data[4] << 24) | (data[5] << 16) | (data[6] << 8) | data[7]);
            byte[] payload = new byte[length - 6];
            Array.Copy(data, 8, payload, 0, payload.Length);

            return new TCPPacket(packetId, sessionId, payload, flags);
        }
    }
}
