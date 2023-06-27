#if OS_WINDOWS
using System.Net;
using System.Runtime.Serialization;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using WallMonitor.Common;
using WallMonitor.Common.Models;
using WallMonitor.Common.Sdk;
using WallMonitor.Monitors.Windows;

namespace WallMonitor.Monitors
{
    /// <summary>
    /// Checks to see if a windows service is running
    /// </summary>
    public sealed class WindowsServiceMonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Windows;
        public string ServiceName => "Windows Service";
        public string ServiceDescription => "Monitors Windows service status.";
        public int Iteration { get; private set; }

        public string DisplayName => $"SVC-{WindowsServiceName}";
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string WindowsServiceName { get; set; } = string.Empty;
        public string ConfigurationDescription => $"Service: {WindowsServiceName}";
        
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public WindowsServiceMonitorAsync(ILogger logger)
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
                TimeoutMilliseconds = 1000;

            var response = HostResponse.Create();
            try
            {
                var startTime = DateTime.UtcNow;
                var username = "";
                var password = "";
                var domain = "";

                if (parameters.Any())
                {
                    if (parameters.Contains("Service"))
                        WindowsServiceName = parameters.Get("Service") ?? string.Empty;
                    if (parameters.Contains("Username"))
                        username = parameters.Get("Username");
                    if (parameters.Contains("Password"))
                        password = parameters.Get("Password");
                    if (parameters.Contains("Domain"))
                        domain = parameters.Get("Domain");
                }

                if (string.IsNullOrEmpty(WindowsServiceName))
                {
                    _logger.LogError("No Windows Service name (configuration name 'Service') provided!");
                    response.IsUp = false;
                    return response;
                }

                var ipAddress = host.Ip ?? Util.HostToIp(host);

                if (Equals(ipAddress, IPAddress.None))
                    ipAddress = IPAddress.Loopback;

                try
                {
                    var action = () =>
                    {
                        ServiceController sc;
                        // check details on the windows service
                        if (IPAddress.IsLoopback(ipAddress))
                            sc = new ServiceController(WindowsServiceName);
                        else
                            sc = new ServiceController(WindowsServiceName, ipAddress.ToString());
                        response.Value = (int)sc.Status;
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            response.IsUp = true;
                        }
                        response.State = sc.DisplayName;
                        sc.Close();
                    };
                    if (!string.IsNullOrEmpty(username))
                    {
                        // run code as this user if required
                        var impersonator = new Impersonator(action, username, password, domain);
                    }
                    else
                    {
                        action.Invoke();
                    }
                }
                catch (InvalidOperationException)
                {
                    // unable to find service
                    response.IsUp = false;
                }
                finally
                {
                    response.ResponseTime = DateTime.UtcNow - startTime;
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
            public string? Service { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
            public string? Domain { get; set; }
        }
    }
}
#endif