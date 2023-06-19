using SystemMonitor.Common.IO.Messages;

namespace SystemMonitor.Common.IO.AgentMessages
{
    public class BeginEventsReceiveMessage : IAgentMessage
    {
        public const ushort ExpectedHeader = 0xFF22;
        public ushort Header => ExpectedHeader;
        public EncryptionTypes EncryptionType { get; }
        public uint Length { get; set; }

        public BeginEventsReceiveMessage(EncryptionTypes encryptionType)
        {
            EncryptionType = encryptionType;
        }

        public uint ComputeLength()
        {
            // compute the length of this message
            uint length = 0;
            length += sizeof(ushort); // header
            length += sizeof(byte); // encryption type
            length += sizeof(uint); // length
            // message contains no data
            return length;
        }

        public byte[] ToBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Header);
            writer.Write((byte)EncryptionType);
            writer.Write(ComputeLength());
            return stream.ToArray();
        }
    }
}
