using System.Net;
using Microsoft.Extensions.Logging;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors.NeedsUpdates
{
    /// <summary>
    /// Checks to see if an IP is inside an RBL list
    /// </summary>
    public class RblMonitorAsync : IMonitorAsync
    {
        //http://www.spamhaus.org/pbl/
        //http://www.spamhaus.org/lookup/
        //http://www.anti-abuse.org/multi-rbl-check/
        //https://rblwatcher.com/rbl-watcher-api#blacklistedip
        public string ServiceName => "RBL List";
        public string ServiceDescription => "Monitors domain for existence in RBL lsits.";
        public int Iteration { get; private set; }

        public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : "RBL";
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string Name { get; set; }
        public string DnsServer { get; set; }
        public List<string> RBLServers { get; set; }
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})\r\nDNS Server: {DnsServer}\r\nRBL List: {FailedRBLServer}";
        public string Host { get; set; }
        public IPAddress HostAddress { get; set; }
        public string FailedRBLServer { get; set; }
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public RblMonitorAsync(ILogger logger)
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
                var startTime = DateTime.UtcNow;
                var rblServers = "";
                RBLServers = new List<string>();
                if (parameters != null)
                {
                    // load config values
                    Name = parameters.Get("name");
                    DnsServer = parameters.Get("dnsServer");

                    rblServers = parameters.Get("rblServers");
                    var serverList = rblServers.Split(new[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
                    RBLServers.Clear();
                    foreach (var server in serverList)
                        RBLServers.Add(server);
                }

                // resolve the host IP (cached)
                IPAddress address = host.Ip;

                if (host.Ip == null || host.Ip == IPAddress.None)
                    address = Util.HostToIp(host);

                var dnsServer = IPAddress.None;
                var dnsHost = new Host(DnsServer, DnsServer, DnsServer);
                if (dnsHost.Ip == null || dnsHost.Ip == IPAddress.None)
                    dnsHost.Ip = Util.HostToIp(dnsHost);

                // if we have an IP, do the RBL check
                if (address != null && address != IPAddress.None)
                {
                    var addressBytes = address.GetAddressBytes();
                    var ipReversed = $"{addressBytes[3]}.{addressBytes[2]}.{addressBytes[1]}.{addressBytes[0]}";
                    var isUp = true;
                    foreach (string rblServer in RBLServers)
                    {
                        // Example: A record zone check 1.0.0.127.zen.spamhaus.org
                        var zoneCheck = $"{ipReversed}.{rblServer}";
                        var responseRecord = DnsTools.QueryARecords(dnsHost.Ip, zoneCheck, TimeoutMilliseconds);
                        // if no A records come back for the domain, it is not in the RBL list.
                        // if A records come back, it's in the RBL list and our check has failed!
                        if (responseRecord.Count > 0)
                        {
                            isUp = false;
                            FailedRBLServer = rblServer;
                        }
                    }
                    response.IsUp = isUp;
                    response.ResponseTime = DateTime.UtcNow - startTime;
                }

            }
            catch (Exception)
            {
                response.IsUp = false;
            }
            finally
            {

            }
            return response;
        }

        public void Dispose()
        {

        }
    }
}
