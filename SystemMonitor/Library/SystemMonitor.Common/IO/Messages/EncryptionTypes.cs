namespace SystemMonitor.Common.IO.Messages
{
    /// <summary>
    /// Encryption type
    /// </summary>
    public enum EncryptionTypes : byte
    {
        Unencrypted = 1,
        /// <summary>
        /// Message is encoded using AES with a 256-bit key
        /// </summary>
        Aes256
    }
}
