using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SystemMonitor.Common;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    public sealed class HttpsMonitorAsync : IMonitorAsync
    {
        public string ServiceName => "HTTPS";
        public string ServiceDescription => "Monitors HTTPS service response and certificate status.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public bool AllowAnyCertificate { get; set; }
        public bool AllowNameMismatch { get; set; }
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
        public SslProtocols Protocol { get; set; }

        public Uri? HostUrl { get; set; }
        public string ConfigurationDescription => $"Host: {HostUrl}";
        private readonly ILogger _logger;

        public HttpsMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {

        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 5000;
            var response = HostResponse.Create();
            var startTime = DateTime.UtcNow;
            Socket? socket = null;
            SslStream? ssl = null;
            NetworkStream? networkStream = null;
            var port = 0;
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
                    AllowAnyCertificate = parameters.Get<bool>("allowAnyCertificate");
                    AllowNameMismatch = parameters.Get<bool>("allowNameMismatch");
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
                    var protocolStr = parameters.Get("protocol");
                    Protocol = SslProtocols.None;
                    if (!string.IsNullOrEmpty(protocolStr))
                    {
                        if (Enum.TryParse<SslProtocols>(protocolStr, true, out SslProtocols protocol))
                            Protocol = protocol;
                    }
                }

                if (HostUrl == null) throw new InvalidOperationException($"{nameof(HttpsMonitorAsync)} configuration expects a valid Url - none was provided!");

                var ipAddress = host.Ip ?? Util.HostToIp(host);
                if (ResolveUrl || Equals(ipAddress, IPAddress.None))
                    ipAddress = Util.GetIpFromHostname(HostUrl);

                if (!Equals(ipAddress, IPAddress.None))
                {
                    var isSuccessful = false;
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var result = socket.BeginConnect(ipAddress, port > 0 ? port : 443, null, null);
                    var complete = result.AsyncWaitHandle.WaitOne((int)TimeoutMilliseconds, true);
                    if (complete && socket.Connected)
                    {
                        networkStream = new NetworkStream(socket);
                        //ssl = new SslStream(networkStream, true, ValidateServerCertificate, ValidateLocalServerCertificate);
                        ssl = new SslStream(networkStream, true, ValidateServerCertificate, null);
                        await ssl.AuthenticateAsClientAsync(HostUrl.Scheme + "://" + HostUrl.Host, null, Protocol, false);

                        // connected! ask for a page
                        if (!HostUrl.IsAbsoluteUri)
                        {
                            // try to repair the hostname if a protocol was not specified
                            HostUrl = new Uri($"https://{HostUrl.OriginalString}");
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
                        ssl.Write(buffer);
                        var so = new SocketObject();
                        so.WorkSocket = socket;
                        so.Ssl = ssl;
                        so.NetworkStream = networkStream;

                        ssl.BeginRead(so.Buffer, 0, so.Buffer.Length, ReceiveCallback, so);
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
                _logger.LogError(ex, $"Exception thrown in '{nameof(HttpsMonitorAsync)}'");
                response.IsUp = false;
            }
            finally
            {
                if (ssl != null)
                {
                    ssl.Close();
                    await ssl.DisposeAsync();
                }
                if (networkStream != null)
                {
                    networkStream.Close();
                    await networkStream.DisposeAsync();
                }
                if (socket?.Connected == true)
                    socket.Close();
                socket?.Dispose();
            }
            return response;
        }

        private X509Certificate ValidateLocalServerCertificate(object obj1, string str1, X509CertificateCollection? col1, X509Certificate? cert1, string[] args)
        {
            return cert1;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch && AllowNameMismatch)
                return true;

            return AllowAnyCertificate;
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            var so = (SocketObject?)result.AsyncState;
            if (so == null)
                return;
            var s = so.WorkSocket;
            var ssl = so.Ssl;
            if (s == null || ssl == null)
                return;
            try
            {
                var read = ssl.EndRead(result);
                if (read > 0)
                {
                    so.Sb.Append(Encoding.Default.GetString(so.Buffer, 0, read));
                    // we only care about the header
                    if (so.Sb.ToString().Contains("\r\n\r\n"))
                    {
                        ssl?.Close();
                        so?.AllDone?.Set();
                    }
                    else
                    {
                        ssl.BeginRead(so.Buffer, 0, so.Buffer.Length, new AsyncCallback(ReceiveCallback), so);
                    }
                }
                else
                {
                    ssl?.Close();
                    so?.AllDone?.Set();
                }
            }
            catch (Exception)
            {
                ssl?.Close();
                try
                {
                    so?.AllDone?.Set();
                }
                catch (Exception)
                {
                    // waithandle was closed
                }
            }
        }

        private class SocketObject : IDisposable
        {
            private const int BufferSize = 256;
            public readonly ManualResetEvent AllDone = new(false);
            public Socket? WorkSocket;
            public SslStream? Ssl;
            public NetworkStream? NetworkStream;
            public readonly byte[] Buffer = new byte[BufferSize];
            public readonly StringBuilder Sb = new();

            public void Dispose()
            {
                AllDone?.Close();
                WorkSocket?.Close();
                WorkSocket?.Dispose();
                Ssl?.Close();
                Ssl?.Dispose();
                NetworkStream?.Close();
                NetworkStream?.Dispose();
                Sb?.Clear();
            }
        }
    }
}
