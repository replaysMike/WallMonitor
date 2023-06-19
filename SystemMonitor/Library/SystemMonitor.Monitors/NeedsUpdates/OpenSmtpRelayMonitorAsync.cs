using System.Net;
using Microsoft.Extensions.Logging;
using SystemMonitor.Common;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors.NeedsUpdates
{
    /// <summary>
    /// Checks to see if a given SMTP server is an open relay
    /// </summary>
    public class OpenSmtpRelayMonitorAsync : IMonitorAsync
    {
        public string ServiceName => "SMTP Open Relay";
        public string ServiceDescription => "Monitors SMTP open relay status.";
        public int Iteration { get; private set; }

        public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : "SMTPRELAY";
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string Name { get; set; }
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})";
        public string Host { get; set; }
        public IPAddress HostAddress { get; set; }
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public OpenSmtpRelayMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 5000;
            var response = HostResponse.Create();
            try
            {
            }
            catch (Exception)
            {
            }
            finally
            {
                response.IsUp = false;
            }
            return response;
        }

        public void Dispose()
        {

        }
    }
}
