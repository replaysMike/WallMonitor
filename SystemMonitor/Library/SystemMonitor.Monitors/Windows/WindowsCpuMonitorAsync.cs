#if OS_WINDOWS
using Microsoft.Extensions.Logging;
using System.Management;
using System.Net;
using System.Runtime.Serialization;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;
using EnumerationOptions = System.Management.EnumerationOptions;

namespace SystemMonitor.Monitors
{
    /// <summary>
    /// Monitor windows cpu usage via WMI.
    /// A better cross-platform alternative is the AgentMonitor
    /// </summary>
    public sealed class WindowsCpuMonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Windows;
        public string ServiceName => "CPU";
        public string ServiceDescription => "Monitors CPU usage";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ConfigurationDescription => $"CPU: {CpuValue}";
        public string CpuValue { get; set; } = string.Empty;
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public WindowsCpuMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {

        }

        private enum SampleMode
        {
            Average,
            SingleCore
        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, System.Threading.CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 1000;

            var response = HostResponse.Create();
            try
            {
                var startTime = DateTime.UtcNow;
                var wmiQuery = "SELECT * FROM Win32_PerfFormattedData_PerfOS_Processor";
                var username = "";
                var password = "";
                var domain = "";
                var matchType = "Value < 0.8";
                var sampleMode = SampleMode.Average;

                // Load monitor configuration parameters
                if (parameters.Any())
                {
                    if (parameters.Contains("MatchType"))
                        matchType = parameters.Get<string>("MatchType");
                    if (parameters.Contains("Username"))
                        username = parameters.Get("Username");
                    if (parameters.Contains("Password"))
                        password = parameters.Get("Password");
                    if (parameters.Contains("Domain"))
                        domain = parameters.Get("Domain");
                    if (parameters.Contains("SampleMode"))
                    {
                        var sampleModeStr = parameters.Get("SampleMode");
                        if (!string.IsNullOrEmpty(sampleModeStr))
                        {
                            Enum.TryParse(sampleModeStr, true, out sampleMode);
                        }
                    }
                }

                // resolve the host IP (cached)
                var ipAddress = host.Ip ?? Util.HostToIp(host);

                // if we have an IP, do the thing
                if (Equals(ipAddress, IPAddress.None))
                    ipAddress = IPAddress.Loopback;

                var options = new ConnectionOptions();
                options.Timeout = TimeSpan.FromMilliseconds(TimeoutMilliseconds);
                if (!IPAddress.IsLoopback(ipAddress))
                {
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        options.Username = username.Trim();
                        options.Password = password;
                    }
                    if (!string.IsNullOrEmpty(domain))
                        options.Authority = $"ntlmdomain:{domain}";
                }

                try
                {
                    var scope = new ManagementScope($@"\\{ipAddress.ToString()}\root\cimv2", options);
                    scope.Connect();
                    if (scope.IsConnected && !string.IsNullOrEmpty(wmiQuery))
                    {
                        var q = new SelectQuery(wmiQuery);
                        var enumOptions = new EnumerationOptions(null, TimeSpan.FromMilliseconds(TimeoutMilliseconds), 1, true, false, true, true, false, true, true);
                        var searcher = new ManagementObjectSearcher(scope, q, enumOptions);

                        var coreSamples = new List<Core>();
                        foreach (var item in searcher.Get())
                        {
                            var id = item.Properties["Name"].Value.ToString();
                            var val = (ulong)item.Properties["PercentProcessorTime"].Value;
                            coreSamples.Add(new Core() { Id = id, Percent = val });
                        }

                        var value = 0d;
                        switch (sampleMode)
                        {
                            case SampleMode.Average:
                                value = (int)(coreSamples.FirstOrDefault(x => x.Id == "_Total")?.Percent ?? 0d) / 100d;
                                break;
                            case SampleMode.SingleCore:
                                value = (int)(coreSamples.Where(x => x.Id != "_Total").OrderByDescending(x => x.Percent).FirstOrDefault()?.Percent ?? 0d) / 100d;
                                break;
                        }

                        response.IsUp = MatchComparer.Compare("Value", value, matchType);
                        CpuValue = $"{value:n2}%";
                        response.Value = value;
                        response.State = value;
                        response.Units = Units.Percentage;
                        response.Range = "0-1.0";
                        response.ResponseTime = DateTime.UtcNow - startTime;

                    }


                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    // WMI connection error
                    response.IsUp = false;
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
            public string? Username { get; set; }
            public string? Password { get; set; }
            public string? Domain { get; set; }
            public string? SampleMode { get; set; }
            [MatchTypeVariables("Value")]
            public string? MatchType { get; set; }
        }

        private class Core
        {
            public string Id { get; init; }
            public ulong Percent { get; init; }
        }
    }
}
#endif