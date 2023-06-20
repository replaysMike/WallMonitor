using System.Net;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors.NeedsUpdates
{
    /// <summary>
    /// Checks for the existence of a network accessible file
    /// </summary>
    public sealed class NetworkFileMonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Application;
        public string ServiceName => "Network File";
        public string ServiceDescription => "Monitors existence of a network accessible file.";
        public int Iteration { get; private set; }

        public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : "FILE";
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string Name { get; set; }
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})";
        public string Host { get; set; }
        public IPAddress HostAddress { get; set; }
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public NetworkFileMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {

        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 5000;
            IHostResponse response = HostResponse.Create();
            try
            {
                if (parameters != null)
                {
                    Name = parameters.Get("Name");
                }

            }
            catch (Exception)
            {
                response.IsUp = false;
            }

            return response;
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            public string? Name { get; set; }
        }
    }
}
