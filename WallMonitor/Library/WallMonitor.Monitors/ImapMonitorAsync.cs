using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WallMonitor.Common;
using WallMonitor.Common.Models;
using WallMonitor.Common.Sdk;

namespace WallMonitor.Monitors
{
    public class ImapMonitorAsync : IMonitorAsync
    {
        public const int DefaultPort = 993;
        public MonitorCategory Category => MonitorCategory.Application;
        public string ServiceName => "IMAP";
        public string ServiceDescription => "Monitors IMAP email service availability.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public bool AllowAnyCertificate { get; set; }
        public bool AllowNameMismatch { get; set; }
        public SslProtocols Protocol { get; set; }
        public string ConfigurationDescription => $"Host: {Host} ({HostAddress})\r\nUsername: {Username}";
        public string? Host { get; set; }
        public IPAddress? HostAddress { get; set; }
        public string? Username { get; set; }
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public ImapMonitorAsync(ILogger logger)
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
            NetworkStream? networkStream = null;
            SslStream? ssl = null;
            try
            {
                // try to get the IP for the host (cached)
                var address = Util.HostToIp(host);
                Host = host?.Hostname?.OriginalString;
                HostAddress = address;

                // if we have an IP, do the thing
                if (!Equals(address, IPAddress.None))
                {
                    if (parameters.Any())
                    {
                        var port = parameters.Get<int>("Port", DefaultPort);
                        var username = parameters.Get<string>("Username");
                        var password = parameters.Get<string>("Password");
                        Username = username;
                        AllowAnyCertificate = parameters.Get<bool>("AllowAnyCertificate", true);
                        AllowNameMismatch = parameters.Get<bool>("AllowNameMismatch", true);
                        var protocolStr = parameters.Get<string>("Protocol");
                        Protocol = SslProtocols.Tls12;
                        if (!string.IsNullOrEmpty(protocolStr))
                        {
                            if (Enum.TryParse<SslProtocols>(protocolStr, true, out SslProtocols protocol))
                                Protocol = protocol;
                        }

                        // initiate connection
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        var result = socket.BeginConnect(address, port > 0 ? port : DefaultPort, null, null);
                        var complete = result.AsyncWaitHandle.WaitOne((int)TimeoutMilliseconds, true);
                        if (complete && socket.Connected)
                        {
                            networkStream = new NetworkStream(socket);
                            ssl = new SslStream(networkStream, false, ValidateServerCertificate, null);
                            await ssl.AuthenticateAsClientAsync(host.Hostname.OriginalString, null, Protocol, false);
                            // say hello
                            var welcomeResponse = ReceiveResponse(socket, ssl, "");
                            // response:	* OK mail.example.com MailSite IMAP4 Server 10.2.0.0 ready
                            var welcomeOk = ParseWelcome(welcomeResponse);

                            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                            {
                                // if we were asked to validate an IMAP account, do so.
                                var loginResult = ReceiveResponse(socket, ssl, string.Format("$ LOGIN {0} {1} {2}", username, password, Environment.NewLine));
                                // response:	$ OK LOGIN completed
                                var loginOk = ParseLogin(loginResult);
                                if (loginOk)
                                {
                                    response.IsUp = true;
                                    response.ResponseTime = DateTime.UtcNow - startTime;
                                }

                                /*
								// get list of IMAP folders
								string listResult = receiveResponse(socket, ssl, String.Format("$ LIST \"\" \"*\"{0}", Environment.NewLine));
								// response:	* LIST (\Select \Noinferiors) "/" INBOX
								//				* LIST (\Select) "/" "Sent Items"
								//				(..more..)
								//				$ OK LIST completed
								// get list info about inbox (counts/read/flags)
								string inBoxResult = receiveResponse(socket, ssl, String.Format("$ SELECT INBOX{0}", Environment.NewLine));
								// response:	* FLAGS (\Answered \Flagged \Deleted \Seen \Draft)
								//				* 949 EXISTS
								//				* 22 RECENT
								//				* OK [PERMANENTFLAGS (\Answered \Flagged \Deleted \Seen \Draft \*)]
								//				* OK [UNSEEN 928] Message 928 is first unseen
								//				* OK [UIDVALIDITY 731266759] UIDs are valid
								//				$ OK [READ-WRITE] opened #shared/michael/INBOX
								// get the total number of messages in inbox
								string inBoxStatusResult = receiveResponse(socket, ssl, String.Format("$ STATUS INBOX (MESSAGES){0}", Environment.NewLine));
								// response:	* STATUS INBOX (MESSAGES 949)
								//				$ OK STATUS completed
								 */
                            }
                            else
                            {
                                // bye!
                                var logoutResult = ReceiveResponse(socket, ssl, $"$ LOGOUT{Environment.NewLine}");
                                // response:	* BYE IMAP4 Server logging out
                                //				$ OK LOGOUT completed
                                response.IsUp = true;
                                response.ResponseTime = DateTime.UtcNow - startTime;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in '{nameof(ImapMonitorAsync)}'");
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
                if (socket != null && socket.Connected)
                    socket.Close();
                socket?.Dispose();
            }
            return response;
        }

        private bool ValidateServerCertificate(object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch && AllowNameMismatch)
                return true;

            return AllowAnyCertificate;
        }

        private bool ParseWelcome(string str)
        {
            var success = false;
            var parts = str.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 2)
            {
                if (parts[0] == "*" && parts[1].Equals("OK", StringComparison.InvariantCultureIgnoreCase) && str.IndexOf("ready", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    success = true;
                    var serverName = parts[2];
                    var serverVersion = "";
                    // if a server version is provided, grab it
                    if (parts.Length > 4)
                        serverVersion = string.Join(" ", parts, 3, parts.Length - 4);
                }
            }
            return success;
        }

        private bool ParseLogin(string str)
        {
            var success = false;
            var parts = str.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 3)
            {
                if (parts[0] == "$" && parts[1].Equals("OK", StringComparison.InvariantCultureIgnoreCase) && parts[2].Equals("LOGIN", StringComparison.InvariantCultureIgnoreCase) && parts[3].IndexOf("completed", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    success = true;
                }
            }

            return success;
        }

        string ReceiveResponse(Socket socket, SslStream ssl, string command)
        {
            var sb = new StringBuilder();
            try
            {
                if (command != "")
                {

                    if (socket.Connected)
                    {
                        // write a message
                        var dummy = Encoding.ASCII.GetBytes(command);
                        ssl.Write(dummy, 0, dummy.Length);
                    }
                    else
                    {
                        throw new ApplicationException("Imap disconnected.");
                    }
                }
                ssl.Flush();

                // wait for response
                var buffer = new byte[2048];
                var bytesRead = ssl.Read(buffer, 0, 2048);
                sb.Append(Encoding.ASCII.GetString(buffer));
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
            return sb.ToString();
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        [DataContract]
        private class ConfigurationContract
        {
            public int? Port { get; set; } = DefaultPort;
            public string? Username { get; set; }
            public string? Password { get; set; }
            public bool AllowAnyCertificate { get; set; } = true;
            public bool AllowNameMismatch { get; set; } = true;
            public SslProtocols Protocol { get; set; } = SslProtocols.Tls12;
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
                    // we only care about the header
                    if (so.Sb.ToString().Contains("\r\n"))
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

        private class SocketObject : IDisposable
        {
            private const int BufferSize = 256;
            public readonly ManualResetEvent AllDone = new(false);
            public readonly Socket? WorkSocket = null;
            public byte[] Buffer = new byte[BufferSize];
            public readonly StringBuilder Sb = new();
            public void Reset()
            {
                AllDone.Reset();
                Sb.Clear();
                Buffer = new byte[BufferSize];
            }

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
