#if OS_WINDOWS
using Microsoft.Extensions.Logging;
using System.Management;
using System.Net;
using SystemMonitor.Common;
using SystemMonitor.Common.Sdk;
using EnumerationOptions = System.Management.EnumerationOptions;

namespace SystemMonitor.Monitors
{
    /// <summary>
    /// Windows Management Instrumentation query monitor
    /// </summary>
    public sealed class WmiMonitorAsync : IMonitorAsync
    {
        public string ServiceName => "WMI Query";
        public string ServiceDescription => "Monitors WMI (Windows Management Instrumentation) query response.";
        public int Iteration { get; private set; }

        public string DisplayName => $"WMI-{QueryName}";
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string QueryName { get; set; }
        public string QueryResponse { get; set; }
        public string QueryCondition { get; set; }
        public string ConfigurationDescription =>
            $"Host: {Host} ({HostAddress})\r\nWMI Query Name: {QueryName}\r\nQuery Condition: {QueryCondition}\r\nQuery Response: {QueryResponse}";
        public string Host { get; set; }
        public IPAddress HostAddress { get; set; }
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public WmiMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {

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
                var wmiQuery = "";
                var matchResponse = "";
                var username = "";
                var password = "";
                var domain = "";
                var scale = 1d;
                var matchType = "Value < 90";
                Units? units = null;

                // Load monitor configuration parameters
                if (parameters.Any())
                {
                    wmiQuery = parameters.Get("Query");
                    if (parameters.Contains("MatchResponse"))
                        matchResponse = parameters.Get("MatchResponse");
                    if (parameters.Contains("Username"))
                        username = parameters.Get("Username");
                    if (parameters.Contains("Password"))
                        password = parameters.Get("Password");
                    if (parameters.Contains("Domain"))
                        domain = parameters.Get("Domain");
                    if (parameters.Contains("MatchType"))
                        matchType = parameters.Get<string>("MatchType");
                    if (parameters.Contains("Scale"))
                        scale = parameters.Get("Scale");
                    if (parameters.Contains("Units"))
                    {
                        units = parameters.Get<Units>("Units");
                    }
                }


                var ipAddress = host.Ip ?? Util.HostToIp(host);

                // if we have an IP, do the thing
                if (Equals(ipAddress, IPAddress.None))
                    ipAddress = IPAddress.Loopback;

                var options = new ConnectionOptions
                {
                    Timeout = TimeSpan.FromMilliseconds(TimeoutMilliseconds)
                };
                if (!IPAddress.IsLoopback(ipAddress))
                {
                    if (!string.IsNullOrEmpty(username))
                    {
                        options.Username = username.Trim();
                        options.Password = password;
                    }
                    if (!string.IsNullOrEmpty(domain))
                        options.Authority = $"ntlmdomain:{domain}";
                }

                try
                { 
                    var scope = new ManagementScope($@"\\{ipAddress}\root\cimv2", options);
                    scope.Connect();
                    if (scope.IsConnected && wmiQuery != null && wmiQuery.Length > 0)
                    {
                        var q = new SelectQuery(wmiQuery);
                        var enumOptions = new EnumerationOptions(null, TimeSpan.FromMilliseconds(TimeoutMilliseconds), 1, true, false, true, true, false, true, true);
                        var searcher = new ManagementObjectSearcher(scope, q, enumOptions);

                        var collection = searcher.Get();
                        var resultCount = collection.Count;
                        foreach (var item in collection)
                        {
                            // find the property we are interested in. WMI has this weird way of sometimes returning more properties than you asked for
                            var propertyName = string.Empty;
                            foreach (var p in item.Properties)
                            {
                                if (wmiQuery.Contains(p.Name, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    propertyName = p.Name;
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(propertyName))
                            {
                                // access the property
                                if (item[propertyName] != null)
                                {
                                    var valAsStr = item[propertyName].ToString();
                                    QueryResponse = valAsStr;

                                    // compare its value
                                    if (long.TryParse(valAsStr, System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture,
                                            out var matchResponseAsLong))
                                    {
                                        response.IsUp = MatchComparer.Compare("Value", matchResponseAsLong, "Count", resultCount, matchType);
                                        response.Value = matchResponseAsLong * scale;
                                    }
                                    else
                                    {
                                        response.IsUp = MatchComparer.Compare("Value", matchResponse, "Count", resultCount, matchType);
                                        if (matchType.Contains("Count", StringComparison.InvariantCultureIgnoreCase))
                                            response.Value = resultCount;
                                    }
                                }
                            }
                        }

                        response.Units = units ?? Units.Auto;
                        response.ResponseTime = DateTime.UtcNow - startTime;
                    }
                }
                catch (System.Runtime.InteropServices.COMException)
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
    }
}
#endif