using WallMonitor.Common.IO;

namespace WallMonitor.Common.Models
{
    public class MonitorServiceConfiguration
    {
        /// <summary>
        /// List of monitors to connect to
        /// </summary>
        public List<Endpoint> Monitors { get; set; } = new();
    }
}
