using Microsoft.Extensions.Logging;
using System.Buffers.Text;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Encodings.Web;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    public sealed class IpCameraMonitorAsync : IMonitorAsync
    {
        public string ServiceName => "IpCamera";
        public string ServiceDescription => "Monitors an IpCamera stream state.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string Host { get; set; } = string.Empty;
        public GraphType GraphType => GraphType.ResponseTime;
        public Dictionary<string, string> Headers { get; set; } = new();
        public string UserAgent { get; set; } = SystemConstants.UserAgent;
        public VideoProtocols Protocol { get; set; } = VideoProtocols.Rtsp;

        public enum VideoProtocols
        {
            Rtsp,
            Sip,
            Mqtt
        }

        public string ConfigurationDescription => $"NzbGet monitor";
        private int _cSeq = 1;
        private readonly ILogger _logger;

        public IpCameraMonitorAsync(ILogger logger)
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
            // default ports:
            // RTSP=554, SIP=5060/5061
            var port = 554;
            var username = "";
            var password = "";
            var uriStr = "";
            Uri? customUri = null;
            Socket? socket = null;
            try
            {
                if (parameters.Any())
                {
                    if (parameters.Contains("Protocol"))
                        Protocol = parameters.Get<VideoProtocols>("Protocol", VideoProtocols.Rtsp);
                    if (parameters.Contains("Username"))
                        username = parameters.Get<string>("Username");
                    if (parameters.Contains("Password"))
                        password = parameters.Get<string>("Password");
                    if (parameters.Contains("Uri"))
                    {
                        uriStr = parameters.Get<string>("Uri");
                        if (!string.IsNullOrEmpty(uriStr))
                        {
                            customUri = new Uri($"tcp://{uriStr}:{port}");
                        }
                    }

                    if (parameters.Contains("UserAgent"))
                        UserAgent = parameters.Get<string>("UserAgent");
                }

                var ipAddress = host.Ip ?? Util.HostToIp(host);
                if (customUri != null)
                    ipAddress = Util.GetIpFromHostname(customUri);


                if (!Equals(ipAddress, IPAddress.None))
                {
                    var isSuccessful = false;
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var result = socket.BeginConnect(ipAddress, port > 0 ? port : 80, null, null);
                    var complete = result.AsyncWaitHandle.WaitOne((int)TimeoutMilliseconds, true);
                    if (complete && socket.Connected)
                    {
                        // connected!

                        switch (Protocol)
                        {
                            case VideoProtocols.Rtsp:
                                {
                                    port = parameters.Get<int>("port", 554);
                                    var uri = $"rtsp://{host.Hostname?.OriginalString ?? ipAddress.ToString()}:{port}";
                                    var sent = SendRtspOptions(socket, uri);
                                    var message = ReceiveRtspMessage(socket, out var responseCode);
                                    response.State = message;
                                    if (responseCode == 401 && !string.IsNullOrEmpty(message))
                                    {
                                        // must authenticate
                                        var i = message.IndexOf("WWW-Authenticate:", StringComparison.InvariantCultureIgnoreCase);
                                        if (i > 0)
                                        {
                                            i += "WWW-Authenticate:".Length;
                                            // calculate the HTTP-Digest value as (via https://en.wikipedia.org/wiki/Digest_access_authentication)
                                            // HA1 = MD5(username:realm:password)
                                            // HA2 = MD5(method:uri)
                                            // response=MD5(HA1:nonce:HA2)
                                            var eol = message.IndexOf("\r\n", i, StringComparison.Ordinal);
                                            var authInfo = message.Substring(i, eol - i);
                                            var authMethod = authInfo.Split(' ', StringSplitOptions.RemoveEmptyEntries).First();
                                            var realm = GetQuotedChunk(authInfo, "realm");
                                            if (authMethod.Equals("Digest", StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                var nonce = GetQuotedChunk(authInfo, "nonce");
                                                var md5 = MD5.Create();
                                                var ha1Value = username + ":" + realm + ":" + password;
                                                var ha2Value = "OPTIONS" + ":" + uri;
                                                var ha1 = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(ha1Value))).ToLower();
                                                var ha2 = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(ha2Value))).ToLower();
                                                var responseDigest = ha1 + ":" + nonce + ":" + ha2;
                                                var responseDigestHashed = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(responseDigest))).ToLower();
                                                sent = SendRtspOptions(socket, uri, username, realm, nonce, responseDigestHashed);
                                                response.State = ReceiveRtspMessage(socket, out responseCode);
                                            }
                                            else if (authMethod.Equals("Basic", StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                var responseValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                                                sent = SendRtspOptions(socket, uri, username, realm, "", responseValue);
                                                response.State = ReceiveRtspMessage(socket, out responseCode);
                                            }
                                        }
                                    }

                                    if (responseCode == 200)
                                    {
                                        isSuccessful = true;
                                    }

                                    response.Value = responseCode;
                                    break;
                                }
                            case VideoProtocols.Sip:
                                {
                                    port = parameters.Get<int>("port", 5060);
                                    var uri = $"rtsp://{host.Hostname?.OriginalString ?? ipAddress.ToString()}:{port}";
                                    var sent = SendSipOptions(socket, uri);
                                    var message = ReceiveSipMessage(socket, out var responseCode);
                                    
                                    if (responseCode == 200)
                                    {
                                        response.IsUp = true;
                                    }

                                    response.Value = responseCode;
                                    break;
                                }
                            case VideoProtocols.Mqtt:
                                {
                                    // MQTT: http://public.dhe.ibm.com/software/dw/webservices/ws-mqtt/MQTT_V3.1_Protocol_Specific.pdf
                                    // http://www.steves-internet-guide.com/mqtt-protocol-messages-overview/
                                    port = parameters.Get<int>("port", 1883); // encrypted port number: 8883
                                    break;
                                }
                        }

                        response.ResponseTime = DateTime.UtcNow - startTime;
                    }

                    response.IsUp = isSuccessful;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(NzbGetMonitorAsync)}");
                response.IsUp = false;
            }
            finally
            {
                if (socket?.Connected == true)
                    socket.Close();
                socket?.Dispose();
            }
            return Task.FromResult(response);
        }

        private string GetQuotedChunk(string str, string chunkName)
        {
            var i = str.IndexOf(chunkName + "=\"", StringComparison.InvariantCultureIgnoreCase);
            var e = str.IndexOf("\"", i + chunkName.Length + 2, StringComparison.Ordinal);
            var startPos = i + chunkName.Length + 2;
            return str.Substring(startPos, e - startPos);
        }

        private string ReceiveRtspMessage(Socket socket, out int responseCode)
        {
            responseCode = -1;
            var so = new SocketObject(socket);
            socket.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, ReceiveCallback, so);
            so.AllDone.WaitOne(250);
            // using an optimized routine to grab rtsp status code instead of using string split(s)
            var responseMessage = so.Sb.ToString();
            var eol = responseMessage.IndexOf("\r\n", 8, StringComparison.Ordinal);
            var line1 = responseMessage.Substring(0, eol);
            var soc = line1.IndexOf(" ", StringComparison.Ordinal);
            if (soc >= 0 && line1.Length > soc)
            {
                soc += 1;
                var responseCodeStr = line1.Substring(soc, 3);
                if (int.TryParse(responseCodeStr, out responseCode))
                {
                    Debug.WriteLine($"RX: {responseMessage}");
                    return responseMessage;
                }
            }
            return string.Empty;
        }

        private string ReceiveSipMessage(Socket socket, out int responseCode)
        {
            responseCode = -1;
            var so = new SocketObject(socket);
            socket.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, ReceiveCallback, so);
            so.AllDone.WaitOne(250);
            // using an optimized routine to grab sip status code instead of using string split(s)
            var responseMessage = so.Sb.ToString();
            var eol = responseMessage.IndexOf("\r\n", 8, StringComparison.Ordinal);
            var line1 = responseMessage.Substring(0, eol);
            var soc = line1.IndexOf(" ", StringComparison.Ordinal);
            if (soc >= 0 && line1.Length > soc)
            {
                soc += 1;
                var responseCodeStr = line1.Substring(soc, 3);
                if (int.TryParse(responseCodeStr, out responseCode))
                {
                    Debug.WriteLine($"RX: {responseMessage}");
                    return responseMessage;
                }
            }
            return string.Empty;
        }

        private int SendRtspOptions(Socket socket, string uri, string? username = "", string realm = "", string nonce = "", string response = "")
        {
            _cSeq++;
            var request = $"OPTIONS {uri} RTSP/1.0\r\n";
            request += $"CSeq: {_cSeq}\r\n";
            request += $"User-Agent: {UserAgent}\r\n";
            if (!string.IsNullOrEmpty(username))
            {
                //Authorization: Digest username="admin", realm="Login to AMC0008C9H027NGXDD", nonce="172fbc02fc32d8c1a05a23b2482dbc45", uri="rtsp://192.168.1.253:554", response="55dda489242dbaa9f294ab791f02dfec"\r\n
                request += $"Authorization: Digest username=\"{username}\"";
                if (!string.IsNullOrEmpty(realm))
                    request += $", realm=\"{realm}\"";
                if (!string.IsNullOrEmpty(nonce))
                    request += $", nonce=\"{nonce}\"";
                request += $", uri=\"{uri}\",";
                request += $"response=\"{response}\"\r\n";
            }
            request += $"\r\n";
            Debug.WriteLine(request);
            var buffer = Encoding.Default.GetBytes(request);
            return socket.Send(buffer);
        }

        private int SendSipOptions(Socket socket, string uri, string? username = "", string realm = "", string nonce = "", string response = "")
        {
            _cSeq++;
            // RFC3261: https://datatracker.ietf.org/doc/html/rfc3261#page-67
            var request = $"OPTIONS sip:user1@example.com SIP/2.0\r\n";
            request += $"Via: SIP/2.0/UDP example.com;branch=z9hG4bKhjhs8ass877\r\n";
            request += $"Max-Forwards: 0\r\n";
            request += $"To: <sip:user1@example.com>\r\n";
            request += $"From: User <sip:user2@example.com>;tag=1928301774\r\n";
            request += $"Call-ID: a84b4c76e66710\r\n";
            request += $"CSeq: 63104 OPTIONS\r\n";
            request += $"Contact: <sip:user1@example.com>\r\n";
            request += $"Accept: application/sdp\r\n";
            request += $"Content-Length: 0\r\n";
            request += $"\r\n";
            Debug.WriteLine(request);
            var buffer = Encoding.Default.GetBytes(request);
            return socket.Send(buffer);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            var so = (SocketObject?)result.AsyncState;
            if (so == null)
                return;
            var s = so.WorkSocket;
            try
            {
                var read = s.EndReceive(result);
                if (read > 0)
                {
                    so.Sb.Append(Encoding.Default.GetString(so.Buffer, 0, read));
                    if (so.Sb.ToString().Contains("\r\n\r\n"))
                    {
                        so.AllDone.Set();
                    }
                    else
                    {
                        s.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, ReceiveCallback, so);
                    }
                }
                else
                {
                    so.AllDone.Set();
                }
            }
            catch (Exception ex)
            {
                so.AllDone.Set();
            }
        }

        private class SocketObject : IDisposable
        {
            private const int BufferSize = 8192;
            public readonly ManualResetEvent AllDone = new(false);
            public readonly Socket WorkSocket;
            public readonly byte[] Buffer = new byte[BufferSize];
            public readonly StringBuilder Sb = new();

            public SocketObject(Socket socket)
            {
                WorkSocket = socket;
            }

            public void Dispose()
            {
                AllDone.Close();
                Sb.Clear();
            }
        }
    }
}
