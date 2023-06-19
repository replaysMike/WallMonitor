namespace SystemMonitor.Common.IO
{
    public class LimitedConfiguration
    {
        /// <summary>
        /// Message version
        /// </summary>
        public byte MessageVersion { get; set; }

        /// <summary>
        /// The host name of the monitor
        /// </summary>
        public string Monitor { get; set; } = null!;

        public List<LimitedHost> Hosts { get; set; } = new();
    }

    public class LimitedHost
    {
        /// <summary>
        /// The display name of the service
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The host name of the service
        /// </summary>
        public string? HostName { get; set; } = null!;

        /// <summary>
        /// The order in which to display the service
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// True if the server is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The image theme to use
        /// </summary>
        public byte ImageTheme { get; set; }

        /// <summary>
        /// The image size to use
        /// </summary>
        public byte ImageSize { get; set; }

        /// <summary>
        /// List of services being monitored
        /// </summary>
        public List<LimitedService> Services { get; set; } = new();
    }

    public class LimitedService
    {
        /// <summary>
        /// Service name
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// True if the service is enabled
        /// </summary>
        public bool Enabled { get; set; }
    }
}
