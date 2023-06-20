using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Runtime.Serialization;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    /// <summary>
    /// Checks for the existence of a database using an active connection
    /// </summary>
    public class RedisMonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Database;
        private static ConnectionMultiplexer? _redis;
        public string ServiceName => "Redis";
        public string ServiceDescription => "Monitors Redis database with optional query execution.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ConfigurationDescription => $"Host: {Host}\r\nUsername: {Username}";
        public string Host { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public RedisMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, System.Threading.CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 5000;

            var response = HostResponse.Create();
            try
            {
                var endpoint = "";
                if (parameters.Any())
                {
                    if (parameters.Contains("Endpoint"))
                        endpoint = parameters.Get("Endpoint");
                }

                if (!string.IsNullOrEmpty(endpoint))
                {
                    try
                    {
                        // set timeout in seconds
                        var timeoutMsInt = (int)TimeoutMilliseconds;
                        // make sure our timeout is always > 0, or it will wait infinitely
                        if (timeoutMsInt <= 0)
                            timeoutMsInt = 5000;
                        
                        if (_redis == null) _redis = await ConnectionMultiplexer.ConnectAsync(new ConfigurationOptions{ EndPoints = { endpoint }, ConnectTimeout = timeoutMsInt });
                        var db = _redis.GetDatabase();
                        var pong = await db.PingAsync();
                        response.IsUp = true;
                        response.Value = 1;
                        response.ResponseTime = pong;
                    }
                    catch (RedisConnectionException ex)
                    {
                        response.IsUp = false;
                    }
                    catch (RedisException ex)
                    {
                        response.IsUp = false;
                        _logger.LogError(ex, $"Redis exception thrown!");
                    }
                }
            }
            catch (Exception ex)
            {
                response.IsUp = false;
                _logger.LogError(ex, $"Exception thrown in  '{nameof(RedisMonitorAsync)}'");
            }

            return response;
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            public string? ConnectionString { get; set; }
            public string? Query { get; set; }
            [MatchTypeVariables("Value", "Count")]
            public string? MatchType { get; set; }
        }

        public void Dispose()
        {

        }
    }
}
