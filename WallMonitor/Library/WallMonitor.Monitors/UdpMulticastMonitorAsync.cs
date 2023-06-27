using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using WallMonitor.Common;
using WallMonitor.Common.IO;
using WallMonitor.Common.Models;
using WallMonitor.Common.Sdk;

namespace WallMonitor.Monitors
{
    /// <summary>
    /// Checks if a TCP port is listening
    /// </summary>
    public sealed class UdpMulticastMonitorAsync : IMonitorAsync
    {
        public static readonly TimeSpan DefaultMessageReceiveTimeout = TimeSpan.FromMilliseconds(8000);
        public MonitorCategory Category => MonitorCategory.Protocol;
        public Uri? Uri { get; set; }
        public string PortName => Util.GetWellKnownPortName(Uri?.Port ?? 0);
        public string ServiceName => "UDP Multicast";
        public string ServiceDescription => "Monitors that a specified UDP multicast stream is receiving packets.";
        public int Iteration { get; private set; }

        public string DisplayName => $"UDP-{PortName}";
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ConfigurationDescription => $"Uri: {Uri}";
        public string? Host { get; set; }
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public const int DefaultUdpReceiveBufferSize = 1024 * 4; // 4MB, default is 64K
        private const int BufferSize = 1500; // 1 MTU = 1500
        private Socket? _socket;
        private SocketState _socketState;
        private System.Timers.Timer? _readTimeoutTimer;
        private DateTime _lastPacketReceive = DateTime.MinValue;
        private TimeSpan _timeBetweenPackets = TimeSpan.Zero;
        private readonly List<TimeSpan> _packetTimesBetweenChecks = new(300);
        private bool _isDisposed;
        private bool _hasReceivedPacketBetweenChecks;
        private byte[] _lastPacket = new byte[BufferSize];
        private int _lastPacketLength = 0;

        public UdpMulticastMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, System.Threading.CancellationToken cancelToken)
        {
            if (_socket == null)
            {
                InitializeSocket(host, parameters);
            }
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 1000;

            var response = HostResponse.Create();
            response.Units = Units.Time;
            if (_packetTimesBetweenChecks.Any())
                response.ResponseTime = TimeSpan.FromMilliseconds(_packetTimesBetweenChecks.Average(x => x.TotalMilliseconds));
            _packetTimesBetweenChecks.Clear();
            var matchType = "";
            try
            {
                if (parameters.Any())
                {
                    if (parameters.Contains("MatchType"))
                        matchType = parameters.Get<string>("MatchType");
                }

                if (!string.IsNullOrEmpty(matchType))
                {
                    // process match types using either ResponseTime, TimeoutMilliseconds, Data
                    var packetHex = matchType.Contains("Data") ? $"0x{Convert.ToHexString(_lastPacket, 0, _lastPacketLength)}" : string.Empty;
                    response.IsUp = MatchComparer.Compare("ResponseTime", response.ResponseTime.TotalMilliseconds, "TimeoutMilliseconds", TimeoutMilliseconds, "Data", packetHex, matchType);
                }
                else
                {
                    // no match type specified, only alert on no packets sent
                    response.IsUp = _hasReceivedPacketBetweenChecks;
                }

                response.Value = response.ResponseTime < TimeSpan.MaxValue ? response.ResponseTime.TotalMilliseconds : -1;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(TcpPortMonitorAsync)}");
                response.IsUp = false;
            }
            finally
            {
                _hasReceivedPacketBetweenChecks = false;
            }

            return response;
        }

        public object GenerateConfigurationTemplate() => new ConfigurationContract();

        public void Dispose()
        {
            _isDisposed = true;
            _readTimeoutTimer?.Dispose();
            if (_socket?.Connected == true)
                _socket.Close();
            _socket?.Dispose();
        }

        [DataContract]
        private class ConfigurationContract
        {
            public string? Uri { get; set; }
            public int? TimeoutMilliseconds { get; set; }
            [MatchTypeVariables("ResponseTime", "TimeoutMilliseconds", "Data")]
            public string? MatchType { get; set; }
        }

        private void readTimeoutTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // failed to receive data. Reset statistics
            _timeBetweenPackets = TimeSpan.MaxValue;
            _hasReceivedPacketBetweenChecks = false;
            // Debug.WriteLine($"WARN: Failed to receive UDP packet within the specified timeout value of {_readTimeoutTimer.Interval}.");
        }

        private void InitializeSocket(IHost host, IConfigurationParameters parameters)
        {
            TimeoutMilliseconds = parameters.Get<int>("TimeoutMilliseconds", (int)DefaultMessageReceiveTimeout.TotalMilliseconds);
            _readTimeoutTimer = new(TimeoutMilliseconds);
            _readTimeoutTimer.Elapsed += readTimeoutTimer_Elapsed;

            Uri = new Uri(parameters.Get<string>("Uri"));
            var multicastIp = IPAddress.Parse(Uri.Host);
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, Uri.Port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socketState = new SocketState(_socket, BufferSize, remoteEndPoint);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, DefaultUdpReceiveBufferSize);
            //_socket.UseOnlyOverlappedIO = true;
            _socket.Bind(remoteEndPoint);
            // join the multicast address
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastIp, IPAddress.Any));
            _socket.BeginReceiveFrom(_socketState.Buffer, 0, _socketState.BufferSize, SocketFlags.None, ref _socketState.RemoteEndPoint, AsyncReceiveCallback, _socketState);
            _readTimeoutTimer?.Start();
        }

        private void AsyncReceiveCallback(IAsyncResult ar)
        {
            // receive some data
            try
            {
                _readTimeoutTimer?.Stop();
                var socketState = (SocketState)ar.AsyncState;
                var socket = socketState.Socket;
                var read = socket.EndReceiveFrom(ar, ref socketState.RemoteEndPoint);
                if (read > 0)
                {
                    Buffer.BlockCopy(socketState.Buffer, 0, _lastPacket, 0, read);
                    _lastPacketLength = read;
                    _lastPacket = socketState.Buffer;
                    _hasReceivedPacketBetweenChecks = true;
                    _timeBetweenPackets = DateTime.UtcNow.Subtract(_lastPacketReceive);
                    _lastPacketReceive = DateTime.UtcNow;
                    _packetTimesBetweenChecks.Add(_timeBetweenPackets);
                    // Debug.WriteLine($"Received UDP Multicast {read} bytes. TBP: {_timeBetweenPackets}");
                }

                // start another receive
                if (!_isDisposed)
                {
                    _readTimeoutTimer?.Start();
                    socket.BeginReceiveFrom(socketState.Buffer, 0, socketState.BufferSize, SocketFlags.None, ref socketState.RemoteEndPoint, AsyncReceiveCallback, socketState);
                }
            }
            catch (ObjectDisposedException)
            {
                // ignore, we disposed of the socket
            }
            catch (SocketException ex)
            {
                // ignore if the socket was disposed
                _logger.LogWarning(ex, $"[{nameof(UdpListener)}] SocketException {nameof(AsyncReceiveCallback)} ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(UdpListener)}] Exception {nameof(AsyncReceiveCallback)} ");
                throw;
            }
        }
    }
}
