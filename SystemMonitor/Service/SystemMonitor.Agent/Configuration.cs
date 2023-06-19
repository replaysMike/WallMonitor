namespace SystemMonitor.Agent
{
    public class Configuration
    {
        /// <summary>
        /// The Ip address to listen on
        /// </summary>
        public string Ip { get; set; } = "*";

        /// <summary>
        /// The port to run on
        /// </summary>
        public int Port { get; set; } = 3500;

        /// <summary>
        /// The list of ip/subnet addresses to allow connections from.
        /// If none are provided, connections will be allowed from all sources.
        /// </summary>
        public List<string> AllowFrom { get; set; } = new();

        /// <summary>
        /// List of enabled modules
        /// </summary>
        public List<string> Modules { get; set; } = new();

        /// <summary>
        /// True to always monitor the system. False will pause all monitors if there are no connections
        /// </summary>
        public bool AlwaysMonitor { get; set; }

        /// <summary>
        /// Max number of threads to use
        /// </summary>
        public int MaxThreads { get; set; }

        /// <summary>
        /// An encryption key if you wish to encrypt the monitoring data
        /// </summary>
        public string? EncryptionKey { get; set; }
    }
}
