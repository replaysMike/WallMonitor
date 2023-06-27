namespace WallMonitor.Common.IO.Messages
{
    /// <summary>
    /// Message header
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Message version
        /// </summary>
        byte MessageVersion { get; set; }

        /// <summary>
        /// Message type
        /// </summary>
        MessageTypes MessageType { get; set; }

        /// <summary>
        /// Encryption type
        /// </summary>
        EncryptionTypes EncryptionType { get; set; }

        /// <summary>
        /// Length of message packet
        /// </summary>
        ushort Length { get; set; }

        /// <summary>
        /// Get the size of the header
        /// </summary>
        int HeaderSize { get; }
    }
}
