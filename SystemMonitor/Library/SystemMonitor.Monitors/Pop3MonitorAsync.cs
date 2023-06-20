using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    public class Pop3MonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Application;
        public const int DefaultPort = 110;
        public string ServiceName => "POP3";
        public string ServiceDescription => "Monitors POP3 service response and accessibility.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})\r\nUsername: {Username}";
        public string? Host { get; set; }
        public IPAddress? HostAddress { get; set; }
        public string? Username { get; set; }
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public Pop3MonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, System.Threading.CancellationToken cancelToken)
        {
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 5000;

            Iteration++;
            var response = HostResponse.Create();
            var startTime = DateTime.UtcNow;
            try
            {
                // try to get the IP for the host (cached)
                var address = Util.HostToIp(host);
                Host = host?.Hostname?.OriginalString;
                HostAddress = address;

                var welcomeReceived = false;
                // if we have an IP, do the thing
                if (!Equals(address, IPAddress.None))
                {
                    if (parameters.Any())
                    {
                        var port = parameters.Get<int>("port", DefaultPort);
                        var username = parameters.Get<string>("username");
                        var password = parameters.Get<string>("password");
                        Username = username;

                        // initiate connection
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        var result = socket.BeginConnect(address, port > 0 ? port : DefaultPort, null, null);
                        var complete = result.AsyncWaitHandle.WaitOne((int)TimeoutMilliseconds, true);
                        if (complete && socket.Connected)
                        {
                            // check welcome message
                            var so = new SocketObject
                            {
                                WorkSocket = socket
                            };
                            socket.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, ReceiveCallback, so);
                            complete = so.AllDone.WaitOne((int)TimeoutMilliseconds);
                            if (complete)
                            {
                                // look for 220 Server message
                                var responseMessage = so.Sb.ToString();
                                if (!string.IsNullOrEmpty(responseMessage) && responseMessage.IndexOf("+OK ", StringComparison.InvariantCultureIgnoreCase) == 0)
                                {
                                    // welcome received
                                    // authenticate if we are asked to do so
                                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                                    {
                                        // send username
                                        var msg = $"USER {username}{Environment.NewLine}";
                                        var buffer = Encoding.Default.GetBytes(msg);
                                        var bytesSent = socket.Send(buffer);

                                        // wait for response
                                        so.Reset();
                                        if (bytesSent == buffer.Length)
                                        {
                                            socket.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, new AsyncCallback(ReceiveCallback), so);
                                        }
                                        complete = so.AllDone.WaitOne((int)TimeoutMilliseconds);
                                        responseMessage = so.Sb.ToString();
                                        // response: +OK if valid user, -ERR if not valid user
                                        if (!string.IsNullOrEmpty(responseMessage) && responseMessage.IndexOf("+OK ", StringComparison.InvariantCultureIgnoreCase) == 0)
                                        {
                                            // user ok, send password
                                            msg = $"PASS {password}{Environment.NewLine}";
                                            buffer = Encoding.Default.GetBytes(msg);
                                            bytesSent = socket.Send(buffer);

                                            // wait for response
                                            so.Reset();
                                            if (bytesSent == buffer.Length)
                                            {
                                                socket.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, new AsyncCallback(ReceiveCallback), so);
                                            }
                                            complete = so.AllDone.WaitOne((int)TimeoutMilliseconds);
                                            responseMessage = so.Sb.ToString();
                                            // response: +OK if valid password, -ERR if not valid user
                                            if (!string.IsNullOrEmpty(responseMessage) && responseMessage.IndexOf("+OK ", StringComparison.InvariantCultureIgnoreCase) == 0)
                                            {
                                                response.IsUp = true;
                                                response.ResponseTime = DateTime.UtcNow - startTime;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        response.IsUp = true;
                                        response.ResponseTime = DateTime.UtcNow - startTime;
                                    }

                                    // send quit
                                    // user ok, send password
                                    var quitMsg = $"QUIT{Environment.NewLine}";
                                    var quitBuffer = Encoding.Default.GetBytes(quitMsg);
                                    socket.Send(quitBuffer);
                                }
                            }

                            if (socket.Connected)
                                socket.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in '{nameof(Pop3MonitorAsync)}'");
                response.IsUp = false;
            }
            finally
            {
            }
            return response;
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            public int? Port { get; set; } = DefaultPort;
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            var so = (SocketObject?)result.AsyncState;
            if (so == null) return;
            var s = so.WorkSocket;
            try
            {
                var read = 0;
                read = s.EndReceive(result);
                if (read > 0)
                {
                    so.Sb.Append(Encoding.Default.GetString(so.Buffer, 0, read));
                    // we only care about the header
                    if (so.Sb.ToString().Contains("\r\n"))
                    {
                        so.AllDone.Set();
                    }
                    else
                    {
                        s.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, new AsyncCallback(ReceiveCallback), so);
                    }
                }
                else
                {
                    so.AllDone.Set();
                }
            }
            catch (ObjectDisposedException)
            {
                so.AllDone.Set();
            }
            catch (Exception)
            {
                // catch and do something with a different exception
                so.AllDone.Set();
            }
        }

        public void Dispose()
        {

        }

        private class SocketObject
        {
            private const int BufferSize = 256;
            public readonly ManualResetEvent AllDone = new(false);
            public Socket? WorkSocket = null;
            public byte[] Buffer = new byte[BufferSize];
            public readonly StringBuilder Sb = new();
            public void Reset()
            {
                AllDone.Reset();
                Sb.Clear();
                Buffer = new byte[BufferSize];
            }
        }
    }
}
