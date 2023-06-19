using SystemMonitor.Common.IO.Messages;

namespace SystemMonitor.Common.IO.AgentMessages
{
    public class AgentConfigurationMessage : IAgentMessage
    {
        public const ushort ExpectedHeader = 0xFF11;
        public ushort Header => ExpectedHeader;
        public EncryptionTypes EncryptionType { get; set; }
        public uint Length { get; set; }

        public byte MonitorsLength { get; set; }
        public List<MonitorConfiguration> Monitors { get; set; } = new();

        public uint ComputeLength()
        {
            // compute the length of this message
            uint length = 0;
            length += sizeof(ushort); // header
            length += sizeof(byte); // encryption type
            length += sizeof(uint); // length
            // monitors
            length += sizeof(byte);
            foreach (var monitor in Monitors)
            {
                length += sizeof(ushort);
                length += (uint)monitor.Monitor.Length + sizeof(byte); // don't forget the null terminator!
                length += (uint)monitor.Schedule.Length + sizeof(byte); // don't forget the null terminator!
                length += sizeof(byte);
                foreach (var config in monitor.Configuration)
                {
                    length += (uint)config.Key.Length + sizeof(byte); // don't forget the null terminator!
                    length += (uint)config.Value.Length + sizeof(byte); // don't forget the null terminator!
                }
            }

            return length;
        }

        public byte[] ToBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Header);
            writer.Write((byte)EncryptionType);
            writer.Write(ComputeLength());

            writer.Write(MonitorsLength);
            foreach (var monitor in Monitors)
            {
                writer.Write(monitor.MonitorId);
                writer.Write(monitor.Monitor);
                writer.Write(monitor.Schedule);
                writer.Write(monitor.ConfigurationLength);
                foreach (var config in monitor.Configuration)
                {
                    writer.Write(config.Key);
                    writer.Write(config.Value);
                }
            }

            return stream.ToArray();
        }
    }

    public class MonitorConfiguration
    {
        /// <summary>
        /// Unique id of monitor
        /// </summary>
        public ushort MonitorId { get; set; }

        /// <summary>
        /// Name of monitor
        /// </summary>
        public string Monitor { get; set; } = null!;

        /// <summary>
        /// Schedule to run monitor on
        /// </summary>
        public string Schedule { get; set; } = null!;

        /// <summary>
        /// Number of configuration entries
        /// </summary>
        public byte ConfigurationLength { get; set; }

        /// <summary>
        /// Monitor Configuration
        /// </summary>
        public Dictionary<string, string> Configuration { get; set; } = new();
    }
}
