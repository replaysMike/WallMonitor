namespace WallMonitor.Common
{
    public class ServiceConfiguration
    {
        /// <summary>
        /// Display name of monitor service
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Name of monitor
        /// </summary>
        public string Monitor { get; set; } = null!;

        /// <summary>
        /// Unique Id of monitor
        /// </summary>
        public int MonitorId { get; set; }

        /// <summary>
        /// Timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// True to enable monitor
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// True to send communication notifications on failure
        /// </summary>
        public bool Notify { get; set; }

        /// <summary>
        /// Attempts before considering failed
        /// </summary>
        public int Attempts { get; set; } = 1;

        /// <summary>
        /// Schedule as an Interval (00:00:00) OR extended chrontab format (sec min hour day month dayofweek). Example: 0/30 0 0 0 0 0 for every 30 seconds.
        /// </summary>
        public string? Schedule { get; set; }

        /// <summary>
        /// Monitor configuration
        /// </summary>
        public Dictionary<string, object?> Configuration { get; set; } = new();
    }
}
