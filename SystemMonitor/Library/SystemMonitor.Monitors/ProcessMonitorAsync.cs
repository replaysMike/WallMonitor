using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    /// <summary>
    /// Checks for the existence of a process
    /// </summary>
    public sealed class ProcessMonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Application;
        public string ServiceName => "Process";
        public string ServiceDescription => "Monitors existence of a running process executable";
        public int Iteration { get; private set; }

        public string DisplayName => ProcessName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ProcessName { get; set; }
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})\r\nProcess: {ProcessName}";
        public string Host { get; set; }
        public IPAddress HostAddress { get; set; }
        public GraphType GraphType => GraphType.Value;
        public string MatchType { get; set; } = "Value > 0";
        private readonly ILogger _logger;

        public ProcessMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {

        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, System.Threading.CancellationToken cancelToken)
        {
            Iteration++;
            var response = HostResponse.Create();
            try
            {
                var startTime = DateTime.UtcNow;

                // Load monitor configuration parameters
                ProcessName = parameters.Get("Process") ?? string.Empty;
                if (parameters.Contains("Name"))
                    ProcessName = parameters.Get("Name") ?? string.Empty;
                if (parameters.Contains("MatchType"))
                    MatchType = parameters.Get<string>("MatchType");

                if (!string.IsNullOrEmpty(ProcessName))
                {
                    var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ProcessName));

                    // if we are only asked to verify if the process exists, it is up
                    response.IsUp = MatchComparer.Compare("Value", processes.Length, "Threads", processes.Any() ? processes.Max(x => x.Threads.Count) : 0, MatchType);
                    response.Value = processes.Length;
                }
                response.ResponseTime = DateTime.UtcNow - startTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(ProcessMonitorAsync)}");
                response.IsUp = false;
            }

            return response;
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            public string? Process { get; set; }
            [MatchTypeVariables("Value", "Threads")]
            public string? MatchType { get; set; }
        }
    }
}
