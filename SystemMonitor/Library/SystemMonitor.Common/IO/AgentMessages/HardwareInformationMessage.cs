using SystemMonitor.Common.IO.Messages;

namespace SystemMonitor.Common.IO.AgentMessages
{
    public class HardwareInformationMessage : IAgentMessage
    {
        public const ushort ExpectedHeader = 0xFF33;
        public ushort Header => ExpectedHeader;
        public EncryptionTypes EncryptionType { get; set; } = EncryptionTypes.Unencrypted;
        public uint Length { get; set; }

        public double Cpu { get; set; }
        public ulong TotalMemoryInstalled { get; set; }
        public ulong TotalMemoryAvailable { get; set; }
        public byte NumberOfDrives { get; set; }
        public Dictionary<byte, string> Drives { get; set; } = new();
        public Dictionary<byte, ulong> DriveSpaceTotal { get; set; } = new();
        public Dictionary<byte, ulong> DriveSpaceAvailable { get; set; } = new();
        public byte NumberOfMonitors { get; set; }
        public List<ServiceInfo> Monitors { get; set; } = new();

        public uint ComputeLength()
        {
            // compute the length of this message
            uint length = 0;
            length += sizeof(ushort); // header
            length += sizeof(byte); // encryption type
            length += sizeof(uint); // length
            length += sizeof(double);
            length += sizeof(ulong);
            length += sizeof(ulong);
            // drives
            length += sizeof(byte);
            foreach (var drive in Drives)
                length += sizeof(byte) + (uint)drive.Value.Length + sizeof(byte); // don't forget the null terminator!
            foreach (var drive in DriveSpaceTotal)
                length += sizeof(byte) + sizeof(ulong);
            foreach (var drive in DriveSpaceAvailable)
                length += sizeof(byte) + sizeof(ulong);
            // monitors
            length += sizeof(byte);
            foreach (var service in Monitors)
            {
                length += sizeof(int);
                length += sizeof(bool);
                length += sizeof(double);
                length += sizeof(long);
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
            writer.Write(Cpu);
            writer.Write(TotalMemoryInstalled);
            writer.Write(TotalMemoryAvailable);
            writer.Write(NumberOfDrives);
            foreach (var drive in Drives)
            {
                writer.Write(drive.Key);
                writer.Write(drive.Value);
            }

            foreach (var drive in DriveSpaceTotal)
            {
                writer.Write(drive.Key);
                writer.Write(drive.Value);
            }
            foreach (var drive in DriveSpaceAvailable)
            {
                writer.Write(drive.Key);
                writer.Write(drive.Value);
            }
            writer.Write(NumberOfMonitors);
            foreach (var monitor in Monitors)
            {
                writer.Write(monitor.MonitorId);
                writer.Write(monitor.IsUp);
                writer.Write(monitor.Value ?? 0d);
                writer.Write(monitor.ResponseTime);
            }
            return stream.ToArray();
        }
    }

    public class ServiceInfo
    {
        public int MonitorId { get; set; }
        public bool IsUp { get; set; }
        public double? Value { get; set; }
        public long ResponseTime { get; set; }
    }
}
