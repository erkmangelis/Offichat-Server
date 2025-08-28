using System;

namespace Offichat.Network
{
    public class UDPPacket : PacketBase
    {
        public ushort SequenceNumber { get; set; }  // Paket sıralama için
        public uint Timestamp { get; set; }         // Lag compensation / interpolation

        public UDPPacket(byte packetId, uint sessionId, byte[] payload, ushort sequenceNumber = 0, uint timestamp = 0, byte flags = 0)
            : base(packetId, sessionId, payload, flags)
        {
            SequenceNumber = sequenceNumber;
            Timestamp = timestamp;
        }

        public override byte[] ToBytes()
        {
            byte[] buffer = new byte[1 + 1 + 4 + 2 + 4 + Payload.Length]; // PacketId + Flags + SessionId + Seq + Timestamp + Payload
            int offset = 0;
            buffer[offset++] = PacketId;
            buffer[offset++] = Flags;
            buffer[offset++] = (byte)((SessionId >> 24) & 0xFF);
            buffer[offset++] = (byte)((SessionId >> 16) & 0xFF);
            buffer[offset++] = (byte)((SessionId >> 8) & 0xFF);
            buffer[offset++] = (byte)(SessionId & 0xFF);
            buffer[offset++] = (byte)(SequenceNumber >> 8);
            buffer[offset++] = (byte)(SequenceNumber & 0xFF);
            buffer[offset++] = (byte)((Timestamp >> 24) & 0xFF);
            buffer[offset++] = (byte)((Timestamp >> 16) & 0xFF);
            buffer[offset++] = (byte)((Timestamp >> 8) & 0xFF);
            buffer[offset++] = (byte)(Timestamp & 0xFF);
            Array.Copy(Payload, 0, buffer, offset, Payload.Length);
            return buffer;
        }

        public static UDPPacket FromBytes(byte[] data)
        {
            if (data.Length < 12) throw new ArgumentException("Data too short to be a valid UDP packet");

            byte packetId = data[0];
            byte flags = data[1];
            uint sessionId = (uint)((data[2] << 24) | (data[3] << 16) | (data[4] << 8) | data[5]);
            ushort sequenceNumber = (ushort)((data[6] << 8) | data[7]);
            uint timestamp = (uint)((data[8] << 24) | (data[9] << 16) | (data[10] << 8) | data[11]);
            byte[] payload = new byte[data.Length - 12];
            Array.Copy(data, 12, payload, 0, payload.Length);

            return new UDPPacket(packetId, sessionId, payload, sequenceNumber, timestamp, flags);
        }
    }
}
