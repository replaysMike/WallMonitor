namespace SystemMonitor.Common.IO.Server
{
    public class TcpServerConfiguration
    {
        public Uri Uri { get; set; } = null!;

        /// <summary>
        /// The list of ip/subnet addresses to allow connections from.
        /// If none are provided, connections will be allowed from all sources.
        /// </summary>
        public List<string> AllowFrom { get; set; } = new();
    }
}
