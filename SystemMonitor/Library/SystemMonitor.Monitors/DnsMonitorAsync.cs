using System.Net;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    public class DnsMonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Application;
        public string ServiceName => "DNS";
        public string ServiceDescription => "Monitors DNS service query response.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ConfigurationDescription => $"DNS Server: {Host} ({HostAddress})\r\nHostname: {Hostname}";
        public string Host { get; set; } = string.Empty;
        public IPAddress HostAddress { get; set; }
        public string Hostname { get; set; } = string.Empty;
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public DnsMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 1000;
            var response = HostResponse.Create();
            try
            {
                var startTime = DateTime.UtcNow;
                var hostname = "";
                var type = "A";
                var matchType = "";
                if (parameters.Any())
                {
                    // load config values
                    if (parameters.Contains("Host"))
                        hostname = parameters.Get("Host"); // the dns name to query
                    if (parameters.Contains("Type"))
                        type = parameters.Get<string>("Type");
                    if (parameters.Contains("MatchType"))
                        matchType = parameters.Get<string>("MatchType");
                    
                    Host = host.Hostname.OriginalString;
                    // resolve the host IP (cached)
                    var address = Util.HostToIp(host);
                    HostAddress = address;

                    // if we have an IP, do the DNS check
                    if (!Equals(address, IPAddress.None) && !string.IsNullOrEmpty(hostname))
                    {
                        // query the dns server at @address for the hostname
                        switch (type.ToLower())
                        {
                            case "a":
                            {
                                var responseRecords = DnsTools.QueryARecords(address, hostname, TimeoutMilliseconds);
                                if (responseRecords.Any())
                                {
                                    response.State = responseRecords;
                                    if (string.IsNullOrEmpty(matchType))
                                        response.IsUp = true;
                                    else
                                        response.IsUp = MatchComparer.Compare("Values", responseRecords.Select(x => x.IPAddress.ToString()), "Count", responseRecords.Count, matchType);
                                }
                                break;
                            }
                            case "mx":
                            {
                                var responseRecords = DnsTools.QueryMxRecords(address, hostname, TimeoutMilliseconds);
                                if (responseRecords.Any())
                                {
                                    response.State = responseRecords;
                                    if (string.IsNullOrEmpty(matchType))
                                        response.IsUp = true;
                                    else
                                        response.IsUp = MatchComparer.Compare("Values", responseRecords.Select(x => x.DomainName), "Count", responseRecords.Count, matchType);
                                }
                                break;
                            }
                            case "ns":
                            {
                                var responseRecords = DnsTools.QueryNsRecords(address, hostname, TimeoutMilliseconds);
                                if (responseRecords.Any())
                                {
                                    response.State = responseRecords;
                                    if (string.IsNullOrEmpty(matchType))
                                        response.IsUp = true;
                                    else
                                        response.IsUp = MatchComparer.Compare("Values", responseRecords.Select(x => x.DomainName), "Count", responseRecords.Count, matchType);
                                }
                                break;
                            }
                            case "soa":
                            {
                                var responseRecord = DnsTools.QuerySoaRecord(address, hostname, TimeoutMilliseconds);
                                if (responseRecord != null)
                                {
                                    response.State = responseRecord;
                                    if (string.IsNullOrEmpty(matchType))
                                        response.IsUp = true;
                                    else
                                        response.IsUp = MatchComparer.Compare("PrimaryNameServer", responseRecord.PrimaryNameServer, "ResponsibleMailAddress", responseRecord.ResponsibleMailAddress, matchType);
                                }
                                break;
                            }
                        }
                        

                        response.ResponseTime = DateTime.UtcNow - startTime;
                    }
                }
            }
            catch (Exception ex)
            {
                response.IsUp = false;
                _logger.LogError(ex, $"Exception thrown in '{nameof(DnsMonitorAsync)}'");
            }
            return response;
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            public string? Host { get; set; }
            public string? Type { get; set; }
            [MatchTypeVariables("Values", "Count")]
            public string? MatchType { get; set; }
        }

        public void Dispose()
        {

        }
    }
}
