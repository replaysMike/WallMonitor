using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.Serialization;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    /// <summary>
    /// Provides access to a SystemMonitor.Agent remote service
    /// </summary>
    public class AgentMonitorAsync : IMonitorAsync, IAgentMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Special;
        public string ServiceName => "Agent";
        public string ServiceDescription => "Monitors multiple hardware instruments via the remote Agent service";
        public int Iteration { get; private set; }

        public string DisplayName => !string.IsNullOrEmpty(Instrument) ? Instrument : ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})";
        public string Host { get; set; } = string.Empty;
        public string Instrument { get; set; } = "CPU";
        public IPAddress HostAddress { get; set; }
        public IAgentsProvider AgentsProvider { get; set; }
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;
        public bool HasPreviousConnection { get; private set; }

        public AgentMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {

        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, CancellationToken cancelToken)
        {
            if (MonitorId <= 0) throw new InvalidOperationException("MonitorId is not set!");
            var response = HostResponse.Create();
            if (!AgentsProvider.IsConnected(host))
            {
                if (!HasPreviousConnection)
                {
                    // not connected yet, don't return a down message
                    response.IsUp = true;
                    return response;
                }
                // not connected/lost connection
                response.IsUp = false;
                return response;
            }
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 5000;
            HasPreviousConnection = true;
            var startTime = DateTime.UtcNow;
            var latestHardwareInformation = await AgentsProvider.GetHardwareInformationAsync(host);
            if (latestHardwareInformation != null)
            {
                var matchType = "Value > 0.8";
                var argument = "";
                var range = "";
                Units? units = null;
                if (parameters.Any())
                {
                    if (parameters.Contains("Instrument"))
                    {
                        Instrument = parameters.Get<string>("Instrument");
                        // set some defaults for each instrument in case they aren't specified
                        switch (Instrument.ToLower())
                        {
                            case "cpu":
                                matchType = "Value > 0.8";
                                range = "0-1.0";
                                units = Units.Percentage;
                                break;
                            case "memoryavailable":
                                matchType = "Value > 100000000";
                                range = "0-MemoryInstalled";
                                units = Units.Gb;
                                break;
                            case "diskavailable":
                                matchType = "Value > 100000000";
                                range = "0-DiskInstalled";
                                units = Units.Gb;
                                break;
                            case "process":
                                matchType = "Value > 0";
                                units = Units.Value;
                                break;
                        }
                    }

                    if (parameters.Contains("MatchType"))
                        matchType = parameters.Get<string>("MatchType");
                    if (parameters.Contains("Argument"))
                        argument = parameters.Get<string>("Argument");
                    if (parameters.Contains("Range"))
                        range = parameters.Get<string>("Range");
                    if (parameters.Contains("Units"))
                    {
                        units = parameters.Get<Units>("Units");
                    }
                }

                var isUpHandled = false;
                var isResponseTimeHandled = false;
                var value = 0d;
                switch (Instrument)
                {
                    case "CPU":
                        value = latestHardwareInformation.Cpu;
                        response.Value = value;
                        response.Range = range;
                        break;
                    case "MemoryAvailable":
                        value = latestHardwareInformation.TotalMemoryAvailable;
                        response.Value = value;
                        response.Range = range?.Replace("MemoryInstalled", latestHardwareInformation.TotalMemoryInstalled.ToString());
                        break;
                    case "MemoryInstalled":
                        value = latestHardwareInformation.TotalMemoryInstalled;
                        response.Value = value;
                        break;
                    case "DiskAvailable":
                        if (latestHardwareInformation.Drives.Any(x => x.Value.StartsWith(argument, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            var drive = latestHardwareInformation.Drives.FirstOrDefault(x => x.Value.StartsWith(argument, StringComparison.InvariantCultureIgnoreCase));
                            if (latestHardwareInformation.DriveSpaceAvailable.ContainsKey(drive.Key))
                                value = latestHardwareInformation.DriveSpaceAvailable[drive.Key];
                            response.Value = value;
                            if (latestHardwareInformation.DriveSpaceTotal.ContainsKey(drive.Key))
                                response.Range = range?.Replace("DiskInstalled", latestHardwareInformation.DriveSpaceTotal[drive.Key].ToString());
                        }
                        break;
                    case "DiskInstalled":
                        if (latestHardwareInformation.Drives.Any(x => x.Value.StartsWith(argument, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            var drive = latestHardwareInformation.Drives.FirstOrDefault(x => x.Value.StartsWith(argument, StringComparison.InvariantCultureIgnoreCase));
                            value = latestHardwareInformation.DriveSpaceTotal[drive.Key];
                            response.Value = value;
                        }
                        break;
                    default:
                        // handle any monitors that are defined
                        if (latestHardwareInformation.Monitors.Any(x => x.MonitorId == MonitorId))
                        {
                            var monitor = latestHardwareInformation.Monitors.First(x => x.MonitorId == MonitorId);
                            response.Value = monitor.Value;
                            response.ResponseTime = TimeSpan.FromTicks(monitor.ResponseTime);
                            response.IsUp = monitor.IsUp;
                            if (units == null)
                                units = Units.Value;
                            isResponseTimeHandled = isUpHandled = true;
                        }
                        break;
                }

                try
                {
                    response.Units = units ?? Units.Auto;
                    if (!isUpHandled)
                        response.IsUp = MatchComparer.Compare("Value", value, matchType.Replace(",", ""));
                }
                catch (Exception ex)
                {
                    // failed
                    _logger.LogError(ex, $"Exception thrown in '{nameof(AgentMonitorAsync)}'");
                }

                if (!isResponseTimeHandled)
                    response.ResponseTime = DateTime.UtcNow - startTime;
            }

            return response;
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            public string? Instrument { get; set; }
            public string? Argument { get; set; }
            public string? Range { get; set; }
            public Units? Units { get; set; }
        }
    }
}
