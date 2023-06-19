using SystemMonitor.Common.IO.Messages;

namespace SystemMonitor.Common.IO.AgentMessages
{
    public interface IAgentMessage
    {
        ushort Header { get; }

        /// <summary>
        /// Encryption type
        /// </summary>
        EncryptionTypes EncryptionType { get; }

        /// <summary>
        /// Length of message
        /// </summary>
        uint Length { get; }

        /// <summary>
        /// Get the bytes of the message
        /// </summary>
        /// <returns></returns>
        byte[] ToBytes();

        /// <summary>
        /// Compute the length of the message
        /// </summary>
        /// <returns></returns>
        uint ComputeLength();
    }
}
