using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Text;
using WallMonitor.Common;
using WallMonitor.Common.Models;
using WallMonitor.Common.Sdk;

namespace WallMonitor.Monitors
{
    public sealed class HttpMonitorAsync : IMonitorAsync
    {
        public const int DefaultPort = 80;
        public MonitorCategory Category => MonitorCategory.Application;
        public string ServiceName => "HTTP";
        public string ServiceDescription => "Monitors HTTP service response.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get;set; }
        public long TimeoutMilliseconds { get; set; }
        public string Host { get; set; } = string.Empty;
        public GraphType GraphType => GraphType.ResponseTime;

        /// <summary>
        /// True to force resolving of the Url instead of using the IP of the server
        /// </summary>
        public bool ResolveUrl { get; set; }
        public string MatchType { get; set; } = "HttpResponseCode >= 200 AND HttpResponseCode < 400";
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string? Body { get; set; }
        public string UserAgent { get; set; } = SystemConstants.UserAgent;

        public Uri? HostUrl { get; set; }
        public string ConfigurationDescription => $"Host: {HostUrl}";
        private readonly ILogger _logger;

        public HttpMonitorAsync(ILogger logger)
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
            var port = DefaultPort;
            try
            {
                HostUrl = host.Hostname;
                if (parameters.Any())
                {
                    if (HostUrl == null && parameters.Contains("Url"))
                        HostUrl = new Uri(parameters.Get("Url"));
                    if (parameters.Contains("MatchType"))
                        MatchType = parameters.Get<string>("MatchType");
                    port = parameters.Get<int>("port");
                    ResolveUrl = parameters.Get<bool>("ResolveUrl");
                    if (parameters.Contains("Method"))
                        Method = parameters.Get<string>("Method");
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

                if (HostUrl == null) throw new InvalidOperationException($"{nameof(HttpMonitorAsync)} configuration expects a valid Url - none was provided!");

                var ipAddress = host.Ip ?? Util.HostToIp(host);
                if (ResolveUrl || Equals(ipAddress, IPAddress.None))
                    ipAddress = Util.GetIpFromHostname(HostUrl);

                if (!Equals(ipAddress, IPAddress.None))
                {
                    var isSuccessful = false;
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var result = socket.BeginConnect(ipAddress, port > 0 ? port : DefaultPort, null, null);
                    var complete = result.AsyncWaitHandle.WaitOne((int)TimeoutMilliseconds, true);
                    if (complete && socket.Connected)
                    {
                        // connected! ask for a page
                        if (!host.Hostname.IsAbsoluteUri)
                        {
                            // try to repair the hostname if a protocol was not specified
                            host.Hostname = new Uri($"http://{host.Hostname.OriginalString}");
                        }
                        var header = $"{Method} {(HostUrl.IsAbsoluteUri ? HostUrl.PathAndQuery : "/")} HTTP/1.1\r\n";
                        header += $"Host: {(HostUrl.IsAbsoluteUri ? HostUrl.DnsSafeHost : HostUrl.OriginalString)}\r\n";
                        header += $"Content-Length: {Body?.Length ?? 0}\r\n";
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
                        if (bytesSent == buffer.Length)
                        {
                            socket.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, ReceiveCallback, so);
                        }
                        complete = so.AllDone.WaitOne((int)TimeoutMilliseconds);
                        response.ResponseTime = DateTime.UtcNow - startTime;
                        if (complete)
                        {
                            // look for any non 400-500 response
                            var responseMessage = so.Sb.ToString();
                            if (responseMessage.Length > 0)
                            {
                                response.State = responseMessage;
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
                                        response.State = responseCode;
                                        isSuccessful = MatchComparer.Compare("HttpResponseCode", responseCode, "ResponseTime", response.ResponseTime.TotalMilliseconds, MatchType);
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
                _logger.LogError(ex, $"Exception thrown in '{nameof(HttpMonitorAsync)}'");
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
            public bool ResolveUrl { get; set; }
            public string? Method { get; set; }
            public string? UserAgent { get; set; }
            public Dictionary<string, string>? Headers { get; set; }
            public string? Body { get; set; }
            [MatchTypeVariables("Value")]
            public string? MatchType { get; set; }
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
            private const int BufferSize = 256;
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
