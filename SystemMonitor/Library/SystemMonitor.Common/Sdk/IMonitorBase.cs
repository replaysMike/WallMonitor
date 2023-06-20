using SystemMonitor.Common.Models;

namespace SystemMonitor.Common.Sdk
{
    public interface IMonitorBase
    {
        MonitorCategory Category { get; }

        /// <summary>
        /// Unique monitor Id
        /// </summary>
        int MonitorId { get; set; }

        /// <summary>
        /// Get the number of times this monitor has been executed
        /// </summary>
        int Iteration { get; }

        /// <summary>
        /// The friendly name to identify this service
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// The friendly description of the service
        /// </summary>
        string ServiceDescription { get; }

        /// <summary>
        /// The friendly name to display to UI
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The timeout to apply to the monitor operation
        /// </summary>
        long TimeoutMilliseconds { get; set; }

        /// <summary>
        /// A friendly string to display configuration details of the monitor
        /// </summary>
        string ConfigurationDescription { get; }

        /// <summary>
        /// The type of data to graph
        /// </summary>
        GraphType GraphType { get; }
    }
}
