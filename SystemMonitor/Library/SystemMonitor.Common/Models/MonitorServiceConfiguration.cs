using SystemMonitor.Common.IO;

namespace SystemMonitor.Common.Models
{
    public class MonitorServiceConfiguration
    {
        /// <summary>
        /// List of monitors to connect to
        /// </summary>
        public List<Endpoint> Monitors { get; set; } = new();
    }
}
