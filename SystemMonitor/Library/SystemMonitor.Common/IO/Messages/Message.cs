namespace SystemMonitor.Common.IO.Messages
{
    /// <summary>
    /// Udp message header
    /// </summary>
    public class Message : IMessage
    {
        public byte MessageVersion { get; set; }
        
        public MessageTypes MessageType { get; set; }
        
        public EncryptionTypes EncryptionType { get; set; }
        
        public ushort Length { get; set; }

        public int HeaderSize => sizeof(byte) + sizeof(byte) + sizeof(byte) + sizeof(ushort);
    }
}
