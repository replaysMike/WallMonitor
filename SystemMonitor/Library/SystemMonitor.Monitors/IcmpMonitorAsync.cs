using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    public class IcmpMonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Application;
        public string ServiceName => "ICMP";
        public string ServiceDescription => "Monitors ICMP echo response time and availability.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})";
        public string Host { get; set; } = string.Empty;
        public IPAddress HostAddress { get; set; } = IPAddress.None;
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public IcmpMonitorAsync(ILogger logger)
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
                TimeoutMilliseconds = 1000;
            var response = HostResponse.Create();
            var matchType = "";
            try
            {
                if (parameters.Any())
                {
                    if (parameters.Contains("MatchType"))
                        matchType = parameters.Get<string>("MatchType");
                }
                var ping = new Ping();

                // resolve the host IP (cached)
                var address = Util.HostToIp(host);
                HostAddress = address;
                Host = host.ToString() ?? string.Empty;

                // if we have an IP, ping it
                if (!Equals(address, IPAddress.None))
                {
                    var reply = await ping.SendPingAsync(address, (int)TimeoutMilliseconds);
                    response.Units = Units.Time;
                    if (string.IsNullOrEmpty(matchType))
                        response.IsUp = reply.Status == IPStatus.Success;
                    else
                        response.IsUp = MatchComparer.Compare("Value", reply.RoundtripTime, matchType);
                    response.ResponseTime = TimeSpan.FromMilliseconds(reply.RoundtripTime);
                    response.Value = reply.RoundtripTime;
                    response.State = reply.RoundtripTime;
                    if (response.IsUp)
                    {
                        _logger.LogInformation($"'{Host}' is up.");
                    }
                    else
                    {
                        _logger.LogError($"'{Host}' is down.");
                    }
                }
                else
                {
                    _logger.LogWarning($"No IP Address is resolved for '{Host}'");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in '{nameof(IcmpMonitorAsync)}'");
                response.IsUp = false;
            }
            return response;
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            [MatchTypeVariables("Value")]
            public string? MatchType { get; set; }
        }
    }
}
