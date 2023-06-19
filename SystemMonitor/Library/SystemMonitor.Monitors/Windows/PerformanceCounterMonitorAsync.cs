#if OS_WINDOWS
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using SystemMonitor.Common;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    public class PerformanceCounterMonitorAsync : IMonitorAsync
    {
        public string ServiceName => "PerformanceCounter";
        public string ServiceDescription => "Monitors performance counter value.";
        public int Iteration { get; private set; }

        public string DisplayName => Category;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string Category { get; set; } = "Processor";
        public string Counter { get; set; } = "% Processor Time";
        public string Instance { get; set; } = "_Total";
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})\nCounter: {Category}.{Counter}.{Instance}";
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
                    Category = parameters.Get("Category") ?? string.Empty;
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

                if (!string.IsNullOrEmpty(Category))
                {
                    if (_performanceCounter == null)
                    {
                        if (!string.IsNullOrEmpty(Instance))
                            _performanceCounter = new PerformanceCounter(Category, Counter, Instance, true);
                        else
                            _performanceCounter = new PerformanceCounter(Category, Counter, true);
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

    }
}
#endif