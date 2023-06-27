using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using WallMonitor.Common;
using WallMonitor.Common.Models;
using WallMonitor.Common.Sdk;

namespace WallMonitor.Monitors
{
    public sealed class NzbGetMonitorAsync : IMonitorAsync
    {
        public const int DefaultPort = 6789;
        public MonitorCategory Category => MonitorCategory.Application;
        public string ServiceName => "NzbGet";
        public string ServiceDescription => "Monitors NzbGet server state.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string Host { get; set; } = string.Empty;
        public GraphType GraphType => GraphType.ResponseTime;
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string? Body { get; set; }
        public string UserAgent { get; set; } = SystemConstants.UserAgent;

        public string ConfigurationDescription => $"NzbGet monitor";
        private readonly ILogger _logger;

        public NzbGetMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {

        }

        public Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 5000;
            var response = HostResponse.Create();
            var startTime = DateTime.UtcNow;
            var hostUrl = host.Hostname;
            var port = DefaultPort;
            var username = "";
            var password = "";
            try
            {
                if (parameters.Any())
                {
                    if (hostUrl == null && parameters.Contains("Url"))
                        hostUrl = new Uri(parameters.Get("Url"));
                    port = parameters.Get<int>("Port", DefaultPort);
                    if (parameters.Contains("Username"))
                        username = parameters.Get<string>("Username");
                    if (parameters.Contains("Password"))
                        password = parameters.Get<string>("Password");
                    if (parameters.Contains("UserAgent"))
                        UserAgent = parameters.Get<string>("UserAgent");
                    var headers = parameters.Get("Headers");
                    if (headers != null)
                    {
                        foreach (var token in headers)
                        {
                            if (!Headers.ContainsKey(token.Name))
                            {
                                if (token.Name == "UserAgent")
                                    UserAgent = token.Value.Value.ToString();
                                else
                                    Headers.Add(token.Name, token.Value.Value.ToString());
                            }
                        }
                    }
                    if (parameters.Contains("Body"))
                        Body = parameters.Get<string?>("Body");
                }

                var ipAddress = host.Ip ?? Util.HostToIp(host);
                if (Equals(ipAddress, IPAddress.None) && hostUrl != null)
                    ipAddress = Util.GetIpFromHostname(hostUrl);

                if (!Equals(ipAddress, IPAddress.None))
                {
                    var isSuccessful = false;
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var result = socket.BeginConnect(ipAddress, port > 0 ? port : 80, null, null);
                    var complete = result.AsyncWaitHandle.WaitOne((int)TimeoutMilliseconds, true);
                    if (complete && socket.Connected)
                    {
                        // connected! ask for a page
                        var header = $"{Method} / HTTP/1.1\r\n";
                        header += $"Host: {hostUrl?.OriginalString ?? ipAddress.ToString()}:{port}\r\n";
                        if (!string.IsNullOrEmpty(username))
                        {
                            var authEncoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
                            header += $"Authorization: Basic {authEncoded}\r\n";
                        }

                        if (!string.IsNullOrEmpty(UserAgent))
                            header += $"User-Agent: {UserAgent}\r\n";
                        // write additional headers
                        foreach (var hdr in Headers)
                            header += $"{hdr.Key}: {hdr.Value}\r\n";
                        header += "Connection: close\r\n";
                        header += "\r\n";

                        if (!string.IsNullOrEmpty(Body))
                            header += Body;
                        header += "\r\n";

                        var buffer = Encoding.Default.GetBytes(header);
                        var bytesSent = socket.Send(buffer);
                        var so = new SocketObject();
                        so.WorkSocket = socket;
                        socket.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, ReceiveCallback, so);
                        complete = so.AllDone.WaitOne((int)TimeoutMilliseconds);
                        response.ResponseTime = DateTime.UtcNow - startTime;
                        if (complete)
                        {
                            // look for any non 400-500 response
                            var responseMessage = so.Sb.ToString();
                            if (responseMessage.Length > 0)
                            {
                                // using an optimized routine to grab http status code instead of using string split(s)
                                var eol = responseMessage.IndexOf("\r\n", 8, StringComparison.Ordinal);
                                var line1 = responseMessage.Substring(0, eol);
                                var soc = line1.IndexOf(" ", StringComparison.Ordinal);
                                if (soc >= 0 && line1.Length > soc)
                                {
                                    soc += 1;
                                    var responseCodeStr = line1.Substring(soc, 3);
                                    if (int.TryParse(responseCodeStr, out var responseCode))
                                    {
                                        response.Value = responseCode;
                                        isSuccessful = responseCode == 200 && (responseMessage.Contains("Server: nzbget-", StringComparison.InvariantCultureIgnoreCase));
                                    }
                                }
                            }
                        }

                        so.Dispose();
                    }
                    response.IsUp = isSuccessful;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(NzbGetMonitorAsync)}");
                response.IsUp = false;
            }
            return Task.FromResult(response);
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            public string? Url { get; set; }
            public int? Port { get; set; } = DefaultPort;
            public string? Username { get; set; }
            public string? Password { get; set; }
            public string? UserAgent { get; set; }
            public Dictionary<string, string>? Headers { get; set; }
            public string? Body { get; set; }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            var so = (SocketObject?)result.AsyncState;
            if (so == null)
                return;
            var s = so.WorkSocket;
            if (s == null)
                return;
            try
            {
                var read = s.EndReceive(result);
                if (read > 0)
                {
                    so.Sb.Append(Encoding.Default.GetString(so.Buffer, 0, read));
                    // we only care about the header
                    if (so.Sb.ToString().Contains("\r\n\r\n"))
                    {
                        s.Close();
                        so.AllDone.Set();
                    }
                    else
                    {
                        s.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, ReceiveCallback, so);
                    }
                }
                else
                {
                    s.Close();
                    so.AllDone.Set();
                }
            }
            catch (Exception)
            {
                s?.Close();
                so.AllDone.Set();
            }
        }

        private class SocketObject : IDisposable
        {
            private const int BufferSize = 8192;
            public readonly ManualResetEvent AllDone = new(false);
            public Socket? WorkSocket;
            public readonly byte[] Buffer = new byte[BufferSize];
            public readonly StringBuilder Sb = new();

            public void Dispose()
            {
                AllDone.Close();
                WorkSocket?.Close();
                WorkSocket?.Dispose();
                Sb.Clear();
            }
        }
    }
}
