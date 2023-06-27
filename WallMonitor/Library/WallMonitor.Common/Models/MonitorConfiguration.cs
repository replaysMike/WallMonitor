using WallMonitor.Common.Sdk;

namespace WallMonitor.Common.Models
{
    internal class MonitorConfiguration
	{
		internal Guid ScheduleId { get; set; }
		internal IMonitorAsync? Monitor { get; set; }
		internal IConfigurationParameters? ConfigurationParameters { get; set; }
		internal IHost? Host { get; set; }
	}
}
