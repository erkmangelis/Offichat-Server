using System;

namespace Offichat.Network
{
    public abstract class PacketBase
    {
        public byte PacketId { get; set; }
        public byte Flags { get; set; }
        public uint SessionId { get; set; }
        public byte[] Payload { get; set; }

        protected PacketBase(byte packetId, uint sessionId, byte[] payload, byte flags = 0)
        {
            PacketId = packetId;
            SessionId = sessionId;
            Payload = payload ?? Array.Empty<byte>();
            Flags = flags;
        }

        public string GetPayloadAsString() => System.Text.Encoding.UTF8.GetString(Payload);

        public abstract byte[] ToBytes();
    }
}
