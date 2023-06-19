using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Common.Models
{
    internal class MonitorConfiguration
	{
		internal Guid ScheduleId { get; set; }
		internal IMonitorAsync? Monitor { get; set; }
		internal IConfigurationParameters? ConfigurationParameters { get; set; }
		internal IHost? Host { get; set; }
	}
}
