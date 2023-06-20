using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SystemMonitor.Common;
using SystemMonitor.Common.Models;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    /// <summary>
    /// Checks to see if SMTP service is running correctly
    /// </summary>
    public sealed class SmtpMonitorAsync : IMonitorAsync
    {
        public MonitorCategory Category => MonitorCategory.Application;
        public string ServiceName => "SMTP";
        public string ServiceDescription => "Monitors SMTP service response and accessibility.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})\r\nUsername: {Username}";
        public string? Host { get; set; }
        public IPAddress HostAddress { get; set; }
        public string Username { get; set; }
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public SmtpMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 5000;

            var response = HostResponse.Create();
            var startTime = DateTime.UtcNow;
            Socket? socket = null;
            string? heloMessage = null;
            StreamWriter? writer = null;
            StreamReader? reader = null;
            try
            {
                // try to get the IP for the host (cached)
                if (parameters.Contains("Hostname"))
                {
                    Host = parameters.Get<string>("Hostname");
                    HostAddress = Util.GetIpFromHostname(new Uri($"tcp://{Host}"));
                }
                else
                {
                    Host = host?.Hostname?.OriginalString;
                    HostAddress = Util.HostToIp(host);
                }

                // if we have an IP, do the thing
                if (!Equals(HostAddress, IPAddress.None))
                {
                    var isSuccessful = false;
                    var welcomeReceived = false;
                    if (parameters.Any())
                    {
                        var port = parameters.Get<int>("Port", 25);
                        var username = parameters.Get<string>("Username");
                        var password = parameters.Get<string>("Password");
                        var tls = parameters.Get<bool>("Tls", false);
                        Username = username;

                        // initiate connection
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        var result = socket.BeginConnect(HostAddress, port > 0 ? port : tls ? 465 : 25, null, null);
                        var complete = result.AsyncWaitHandle.WaitOne((int)TimeoutMilliseconds, true);
                        if (complete && socket.Connected)
                        {
                            // connected!

                            if (tls)
                            {
                                var sslStream = new SslStream(new NetworkStream(socket), true, ServerValidationCallback, null, EncryptionPolicy.RequireEncryption);
                                sslStream.ReadTimeout = (int)TimeoutMilliseconds;
                                sslStream.WriteTimeout = (int)TimeoutMilliseconds;
                                var hostname = Host;
                                if (string.IsNullOrEmpty(hostname))
                                    hostname = HostAddress.ToString();
                                await sslStream.AuthenticateAsClientAsync(hostname, null, System.Security.Authentication.SslProtocols.Tls12, true);
                                writer = new StreamWriter(sslStream, Encoding.ASCII);
                                reader = new StreamReader(sslStream, Encoding.ASCII);
                            }
                            else
                            {
                                var stream = new NetworkStream(socket);
                                writer = new StreamWriter(stream, Encoding.ASCII);
                                reader = new StreamReader(stream, Encoding.ASCII);
                            }

                            // check welcome message
                            var so = new SocketObject
                            {
                                WorkSocket = socket
                            };
                            // look for 220 Server message
                            var responseMessage = GetReply(reader, out var code);
                            if (code == 220)
                            {
                                // welcome received
                                welcomeReceived = true;
                                heloMessage = responseMessage;
                            }

                            if (welcomeReceived)
                            {
                                var hostname = Host;
                                if (string.IsNullOrEmpty(hostname))
                                    hostname = $"[{HostAddress}]"; // per RFC 2821: specify IP in brackets if no hostname is provided
                                // say hello
                                var bytesSent = SendCommand(writer, $"EHLO {hostname}\r\n");

                                // check for data response
                                responseMessage = GetReply(reader, out code);
                                var supportsPlainAuth = false;
                                var supportsXAuth2 = false;
                                if (code == 250)
                                {
                                    response.Value = code;
                                    // get supported authorizations
                                    var lines = responseMessage.Split("\r\n");
                                    for (var i = 0; i < lines.Length; i++)
                                    {
                                        if (string.Compare(lines[i], 4, "STARTTLS ", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            if (tls)
                                            {
                                                // start TLS mode
                                                bytesSent = SendCommand(writer, "STARTTLS\r\n");
                                                responseMessage = GetReply(reader, out code);
                                            }
                                        }
                                        if (string.Compare(lines[i], 4, "AUTH ", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            // remove the auth text
                                            var authTypes = lines[i].Substring(9).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                            foreach (string authType in authTypes)
                                            {
                                                if (string.Compare(authType, "PLAIN", StringComparison.OrdinalIgnoreCase) == 0) supportsPlainAuth = true;
                                                else if (string.Compare(authType, "XOAUTH2", StringComparison.OrdinalIgnoreCase) == 0) supportsXAuth2 = true;
                                            }
                                        }
                                    }
                                }

                                var isAuthenticated = string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password);
                                if (!isAuthenticated)
                                {
                                    // config specifies we must authenticate
                                    if (supportsPlainAuth)
                                    {
                                        Debug.WriteLine("Plain Auth supported, trying to login...");
                                        // plaintext authentication
                                        bytesSent = SendCommand(writer, "AUTH LOGIN\r\n");
                                        responseMessage = GetReply(reader, out code);
                                        Debug.WriteLine($"Login reply {code}...");
                                        if (code == 334)
                                        {
                                            Debug.WriteLine($"Sending username...");
                                            bytesSent = SendCommand(writer, Convert.ToBase64String(Encoding.ASCII.GetBytes(username)) + "\r\n");
                                            responseMessage = GetReply(reader, out code);
                                            if (code == 334)
                                            {
                                                Debug.WriteLine($"Sending password...");
                                                bytesSent = SendCommand(writer, Convert.ToBase64String(Encoding.ASCII.GetBytes(password)) + "\r\n");
                                                responseMessage = GetReply(reader, out code);
                                                if (code == 235 || code == 534)
                                                {
                                                    // either password succeeded, or further two-factor authentication is required to fully authenticate
                                                    // auth success
                                                    isAuthenticated = true;
                                                    response.Value = code;
                                                }
                                            }
                                        }
                                    }
                                    else if (supportsXAuth2)
                                    {
                                        // OAUth2 authentication not supported
                                        isAuthenticated = false;
                                    }
                                }

                                // send quit
                                bytesSent = SendCommand(writer, $"QUIT\r\n");

                                // receive 221 message
                                responseMessage = GetReply(reader, out code);
                                if (isAuthenticated && code == 221)
                                {
                                    // received, ok to quit
                                    isSuccessful = true;
                                }
                            }

                            if (socket.Connected)
                                socket.Close();
                        }
                        response.IsUp = isSuccessful;
                        response.ResponseTime = DateTime.UtcNow - startTime;
                        response.State = heloMessage;
                        if (writer != null) await writer.DisposeAsync();
                        reader?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in '{nameof(SmtpMonitorAsync)}'");
                response.IsUp = false;
                response.Value = -1;
            }
            finally
            {
                // disconnect if need be
                if (socket != null && socket.Connected)
                    socket.Close();
            }

            return response;
        }

        private int SendCommand(StreamWriter writer, string command)
        {
            writer.Write(command);
            writer.Flush();
            //return socket.Send(Encoding.UTF8.GetBytes(command));
            return command.Length;
        }

        private int SendCommand(Socket socket, string command)
        {
            return socket.Send(Encoding.UTF8.GetBytes(command));
        }

        private string? GetReply(StreamReader reader, out int code)
        {
            var replyStrings = new List<string>();
            var responseMessage = reader.ReadLine();
            if (string.IsNullOrEmpty(responseMessage))
            {
                code = -1;
                return null;
            }

            replyStrings.Add(responseMessage);
            while (responseMessage[3] == '-')
            {
                responseMessage = reader.ReadLine();
                if (responseMessage == null) break;
                replyStrings.Add(responseMessage);
            }

            int.TryParse(replyStrings[0].Substring(0, 3), out code);

            return string.Join("\r\n", replyStrings);
        }

        private string? GetReply(SocketObject so)
        {
            so.Reset();
            so.WorkSocket!.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, ReceiveCallback, so);
            if (so.AllDone.WaitOne((int)TimeoutMilliseconds))
            {
                var responseMessage = so.Sb.ToString();
                return responseMessage;
            }
            return null;
        }

        private bool ServerValidationCallback(object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors policyErrors)
        {
            // save for debugging
            var certificateStr = certificate.ToString();
            var policyErrorsStr = policyErrors.ToString();
            // certificate is accepted
            return true;
        }

        private X509Certificate ClientCertificateSelectionCallback(object? sender, string? targethost, X509CertificateCollection? localcertificates, X509Certificate? remotecertificate, string[] acceptableissuers)
        {
            return null;
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            var so = (SocketObject?)result.AsyncState;
            if (so == null)
                return;
            var s = so.WorkSocket;
            try
            {
                var bytesRead = s.EndReceive(result);
                if (bytesRead > 0)
                {
                    so.Sb.Append(Encoding.Default.GetString(so.Buffer, 0, bytesRead));
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

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            public string? Hostname { get; set; }
            public int? Port { get; set; } = 25;
            public string? Username { get; set; }
            public string? Password { get; set; }
            public bool Tls { get; set; }
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
