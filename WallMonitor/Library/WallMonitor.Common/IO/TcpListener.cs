using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using WallMonitor.Common.IO.Security;

namespace WallMonitor.Common.IO
{
    public class TcpListener : IListener
    {
        private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(5);

        private readonly ILogger _logger;
        private readonly ServerEventPacketProcessor _packetProcessor;
        private ManualResetEvent _isClosing = new(false);
        private Socket? _socket;
        private Thread? _receiveThread;
        private readonly ManualResetEvent _closingEvent = new(false);
        private readonly byte[] _buffer = new byte[4096];
        private bool _isDisposed;

        /// <summary>
        /// Server event update received
        /// </summary>
        public event EventHandler<ServerNotificationEventArgs>? ServerEventReceived;

        /// <summary>
        /// Monitor service configuration
        /// </summary>
        public event EventHandler<MonitorConfigurationEventArgs>? ConfigurationEventReceived;

        /// <summary>
        /// Connection lost event
        /// </summary>
        public event EventHandler<ConnectionLostEventArgs>? ConnectionLost;

        /// <summary>
        /// Connection restored event
        /// </summary>
        public event EventHandler<ConnectionRestoredEventArgs>? ConnectionRestored;

        /// <summary>
        /// Unique Id of the listener
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Name of the monitor service
        /// </summary>
        public string Name { get; }
        public string? IpAddress { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; } = new(0, 0);

        /// <summary>
        /// Udp broadcast address
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// The order in which to display its monitored services in the results
        /// </summary>
        public int OrderId { get; }

        internal AutoResetEvent MessageProcessed = new(false);

        public TcpListener(string name, Uri uri, int orderId, ILogger logger, string? encryptionKey, IAesEncryptionService aesEncryptionService)
        {
            Name = name;
            Uri = uri;
            OrderId = orderId;
            _logger = logger;
            var ip = Util.GetIpFromHostname(uri);
            IpAddress = ip.ToString();
            var ipEndPoint = new IPEndPoint(ip, Uri.Port);
            RemoteEndPoint = ipEndPoint;
            _packetProcessor = new ServerEventPacketProcessor(encryptionKey, aesEncryptionService);
            _packetProcessor.ServerUpdateNotificationReceived += (sender, e) =>
            {
                ServerEventReceived?.Invoke(this, e);
                MessageProcessed.Set();
            };
            _packetProcessor.ConfigurationReceived += (sender, e) =>
            {
                ConfigurationEventReceived?.Invoke(this, e);
                MessageProcessed.Set();
            };
        }

        /// <summary>
        /// Start receiving messages
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Start()
        {
            // connect
            _socket = new Socket(RemoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            while (!_socket.Connected)
            {
                try
                {
                    Console.WriteLine("Connecting...");
                    var startTime = DateTime.UtcNow;
                    var result = _socket.BeginConnect(RemoteEndPoint.Address, RemoteEndPoint.Port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(ConnectionTimeout, true);
                    if (_socket.Connected)
                    {
                        _socket.EndConnect(result);

                        //ClientConnected?.Invoke(this, new ClientEventArgs(ipEndPoint.Address.ToString()));
                        Console.WriteLine("Connected!");
                        //_messageId = 0;

                        _receiveThread = new Thread(ReceiveThreadAsync);
                        _receiveThread.Start();
                    }
                    else
                    {
                        // failed to reconnect, retry
                        Console.WriteLine($"Failed to connect!");
                        var delayAmount = ConnectionTimeout - (DateTime.UtcNow - startTime);
                        if (delayAmount.TotalMilliseconds > 0)
                            Task.Delay(delayAmount).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    // failed to connect, todo: reconnect
                    Console.WriteLine($"Exception on connect thrown: {ex.Message}");
                }
            }
        }

        private async void ReceiveThreadAsync()
        {
            try
            {
                Console.WriteLine("ReceiveThreadAsync() start");

                while (!_closingEvent.WaitOne(50))
                {
                    var receiveLength = await _socket.ReceiveAsync(_buffer, SocketFlags.None);
                    if (receiveLength > 0)
                    {
                        var buffer = new ArraySegment<byte>(_buffer, 0, receiveLength);
                        _packetProcessor.ReadPacket(buffer);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket exception, reconnecting...");
                try
                {
                    _socket?.Shutdown(SocketShutdown.Both);
                    _socket?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
                Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown: {ex.Message}");
                try
                {
                    _socket?.Shutdown(SocketShutdown.Both);
                    _socket?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }

                Start();
            }
            Console.WriteLine("ReceiveThreadAsync() end");
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop()
        {
            _closingEvent.Set();
        }


        public void Dispose()
        {
            Stop();
            // let the socket settle before disposing it
            _socket?.Dispose();
            _packetProcessor.Dispose();
            _isDisposed = true;
        }
    }
}
