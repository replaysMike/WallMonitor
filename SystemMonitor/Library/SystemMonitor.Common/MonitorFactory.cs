using System.Reflection;
using SystemMonitor.Common.Sdk;
using Microsoft.Extensions.DependencyInjection;

namespace SystemMonitor.Common
{
    /// <summary>
    /// Factory for creating instances of IMonitorAsync
    /// </summary>
    public static class MonitorFactory
    {
        private static readonly Lazy<Assembly> MonitorAssembly = new (() => Assembly.Load("SystemMonitor.Monitors"));

        /// <summary>
        /// Create an instance of IMonitorAsync
        /// </summary>
        /// <param name="scope">Service provider scope</param>
        /// <param name="name">Name of monitor</param>
        /// <param name="monitorId">Unique Id for the monitor instance</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IMonitorAsync Create(IServiceScope scope, string name, int monitorId)
        {
            Console.WriteLine($"Creating Monitor '{name}' with Id {monitorId}");
            var type = MonitorAssembly.Value.GetType($"SystemMonitor.Monitors.{name.Replace("MonitorAsync", "", StringComparison.InvariantCultureIgnoreCase)}MonitorAsync", false, true);
            if (type != null && typeof(IMonitorAsync).IsAssignableFrom(type))
            {
                if (scope.ServiceProvider.GetService(type) is IMonitorAsync monitor)
                {
                    monitor.MonitorId = monitorId;
                    return monitor;
                }
            }

            throw new ArgumentException($"Unknown Monitor module '{name}'");
        }
    }
}
