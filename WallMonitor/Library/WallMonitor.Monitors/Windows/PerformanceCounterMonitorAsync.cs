#if OS_WINDOWS
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;
using WallMonitor.Common;
using WallMonitor.Common.Models;
using WallMonitor.Common.Sdk;

namespace WallMonitor.Monitors
{
    public class PerformanceCounterMonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Windows;
        public string ServiceName => "PerformanceCounter";
        public string ServiceDescription => "Monitors performance counter value.";
        public int Iteration { get; private set; }

        public string DisplayName => WmiCategory;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string WmiCategory { get; set; } = "Processor";
        public string Counter { get; set; } = "% Processor Time";
        public string Instance { get; set; } = "_Total";
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})\nCounter: {WmiCategory}.{Counter}.{Instance}";
        public string Host { get; set; }
        public IPAddress HostAddress { get; set; }
        public GraphType GraphType => GraphType.Value;
        public string MatchType { get; set; } = "Value < 90";
        public double Scale { get; set; } = 1;
        private readonly ILogger _logger;
        private PerformanceCounter? _performanceCounter;

        public PerformanceCounterMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            _performanceCounter?.Dispose();
        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, System.Threading.CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 5000;

            var response = HostResponse.Create();
            try
            {
                var startTime = DateTime.UtcNow;
                var range = "";

                // Load monitor configuration parameters
                if (parameters.Contains("Category"))
                {
                    WmiCategory = parameters.Get("Category") ?? string.Empty;
                    Counter = string.Empty;
                    Instance = string.Empty;
                }

                if (parameters.Contains("Counter"))
                {
                    Counter = parameters.Get<string>("Counter");
                    Instance = string.Empty;
                }

                if (parameters.Contains("Instance"))
                    Instance = parameters.Get<string>("Instance");
                if (parameters.Contains("MatchType"))
                    MatchType = parameters.Get<string>("MatchType");
                if (parameters.Contains("Range"))
                    range = parameters.Get<string>("Range");
                if (parameters.Contains("Scale"))
                {
                    var scale = parameters.Get<double>("Scale", -1);
                    if (scale > 0)
                        Scale = scale;
                }

                if (!string.IsNullOrEmpty(WmiCategory))
                {
                    if (_performanceCounter == null)
                    {
                        if (!string.IsNullOrEmpty(Instance))
                            _performanceCounter = new PerformanceCounter(WmiCategory, Counter, Instance, true);
                        else
                            _performanceCounter = new PerformanceCounter(WmiCategory, Counter, true);
                    }

                    // if we are only asked to verify if the process exists, it is up
                    response.Range = range;
                    response.Value = Math.Round(_performanceCounter.NextValue() * Scale);
                    response.IsUp = MatchComparer.Compare("Value", response.Value, MatchType);
                }
                response.ResponseTime = DateTime.UtcNow - startTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(PerformanceCounterMonitorAsync)}");
                response.IsUp = false;
            }

            return response;
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            public string? Category { get; set; }
            public string? Counter { get; set; }
            public string? Instance { get; set; }
            public string? Range { get; set; }
            public double? Scale { get; set; }
            [MatchTypeVariables("Value")]
            public string? MatchType { get; set; }
        }

    }
}
#endif