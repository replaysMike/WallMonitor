namespace WallMonitor.Common.IO.Messages
{
    public static class MessageVersions
    {
        public static ServerStatusUpdateVersions ServerStatusUpdate => new ();
        public static MonitorServiceConfigurationVersions MonitorServiceConfiguration => new ();
    }

    public class ServerStatusUpdateVersions
    {
        public byte Latest => Versions.Last();
        public byte[] Versions => new byte[] { 0x01 };
    }

    public class MonitorServiceConfigurationVersions
    {
        public byte Latest => Versions.Last();
        public byte[] Versions => new byte[] { 0x01 };
    }
}
