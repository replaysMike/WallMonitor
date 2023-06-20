using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    /// <summary>
    /// Checks if a TCP port is listening
    /// </summary>
    public sealed class TcpPortMonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Protocol;
        public int Port { get; set; }
        public string PortName => Util.GetWellKnownPortName(Port);
        public string ServiceName => "TCP Port";
        public string ServiceDescription => "Monitors that a specified TCP port is open.";
        public int Iteration { get; private set; }

        public string DisplayName => $"TCP-{PortName}";
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})\r\nTCP Port: {PortName} ({Port})";
        public string? Host { get; set; }
        public IPAddress HostAddress { get; set; }
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public TcpPortMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, System.Threading.CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 1000;
            var response = HostResponse.Create();
            response.Units = Units.Value;
            response.Range = "1-65535";
            Socket? socket = null;
            try
            {
                /* Connect to required port */
                var startTime = DateTime.UtcNow;
                if (parameters.Any())
                {
                    var portStr = parameters.Get<string>("Port");
                    if (!string.IsNullOrEmpty(portStr))
                    {
                        if (int.TryParse(portStr, out int port) && port > 0)
                            Port = port;
                        else
                        {
                            Port = Util.GetPortFromWellKnown(portStr);
                        }
                    }
                }

                // if a proper port number was found, proceed
                if (Port > 0)
                {
                    // do IP address conversion (cached)
                    var address = Util.HostToIp(host);
                    Host = host.Hostname?.OriginalString;
                    HostAddress = address;

                    if (!Equals(address, IPAddress.None))
                    {
                        // check the port to see if it's connectable
                        response.Value = Port;
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        var result = socket.BeginConnect(address, Port, null, null);
                        var complete = result.AsyncWaitHandle.WaitOne((int)TimeoutMilliseconds, true);
                        if (complete)
                        {
                            // connected! all is good
                            response.IsUp = complete;
                            response.ResponseTime = DateTime.UtcNow - startTime;
                            socket.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(TcpPortMonitorAsync)}");
                response.IsUp = false;
            }
            finally
            {
                if (socket?.Connected == true)
                    socket.Close();
                socket?.Dispose();
            }

            return response;
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        public void Dispose()
        {

        }

        [DataContract]
        private class ConfigurationContract
        {
            public int? Port { get; set; } = 25;
        }
    }
}
