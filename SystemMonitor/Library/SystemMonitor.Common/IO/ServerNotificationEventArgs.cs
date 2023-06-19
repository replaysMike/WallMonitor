using SystemMonitor.Common.Models;

namespace SystemMonitor.Common.IO
{
    public class ServerNotificationEventArgs
    {
        public EventType EventType { get; set; }
        public GraphType GraphType { get; set; }
        public string Host { get; set; } = null!;
        public string Service { get; set; } = null!;
        public SystemAlertLevel AlertLevel { get; set; }
        public string? Message { get; set; }
        public DateTime DateTime { get; set; }
        public ServiceState ServiceState { get; set; }
        public double? Value { get; set; }
        public string? Range { get; set; }
        public Units Units { get; set; }
        public long ResponseTime { get; set; }
        public DateTime LastUpTime { get; set; }
        public TimeSpan PreviousDownTime { get; set; }
    }
}
