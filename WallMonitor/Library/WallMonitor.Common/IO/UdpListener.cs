using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using WallMonitor.Common.IO.Security;

namespace WallMonitor.Common.IO;

/// <summary>
/// Listens for Udp server state messages via Udp multicast
/// </summary>
public class UdpListener : IListener
{
    /// <summary>
    /// UDP receive buffer size. A buffer that is too small will have trouble receiving many small packets.
    /// </summary>
    public const int DefaultUdpReceiveBufferSize = 1024 * 4; // 4MB, default is 64K
    private const int BufferSize = 1500; // 1 MTU = 1500
    private static readonly TimeSpan MessageReceiveTimeout = TimeSpan.FromSeconds(15);

    private readonly ILogger _logger;
    private Socket? _socket;
    private SocketState _socketState;
    private int _udpReceiveBufferSize;
    private readonly ServerEventPacketProcessor _packetProcessor;
    internal AutoResetEvent MessageProcessed = new(false);
    private bool _isDisposed;
    private readonly System.Timers.Timer _readTimeoutTimer = new(MessageReceiveTimeout);
    private bool _connectionLost = false;

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

    /// <summary>
    /// Udp broadcast address
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// The order in which to display its monitored services in the results
    /// </summary>
    public int OrderId { get; }

    public int UdpReceiveBufferSize
    {
        get => _udpReceiveBufferSize;
        set
        {
            _udpReceiveBufferSize = value;
            _socket?.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, _udpReceiveBufferSize);
        }
    }

    public UdpListener(string name, Uri uri, int orderId, ILogger logger, string? encryptionKey, IAesEncryptionService aesEncryptionService) : this(name, uri, orderId, logger, encryptionKey, aesEncryptionService, DefaultUdpReceiveBufferSize)
    {
    }

    public UdpListener(string name, Uri uri, int orderId, ILogger logger, string? encryptionKey, IAesEncryptionService aesEncryptionService, int udpReceiveBufferSize)
    {
        Name = name;
        Uri = uri;
        OrderId = orderId;
        _logger = logger;
        _packetProcessor = new ServerEventPacketProcessor(encryptionKey, aesEncryptionService);
        _udpReceiveBufferSize = udpReceiveBufferSize > 0 ? DefaultUdpReceiveBufferSize : udpReceiveBufferSize;

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
        _readTimeoutTimer.Elapsed += readTimeoutTimer_Elapsed;
        _readTimeoutTimer.Start();
    }

    private void readTimeoutTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // failed to receive data
        ConnectionLost?.Invoke(this, new ConnectionLostEventArgs(Name, Uri));
        _connectionLost = true;
    }

    /// <summary>
    /// Start receiving messages
    /// </summary>
    public void Start()
    {
        var multicastIp = IPAddress.Parse(Uri.Host);
        var remoteEndPoint = new IPEndPoint(IPAddress.Any, Uri.Port);

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socketState = new SocketState(_socket, BufferSize, remoteEndPoint);
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, _udpReceiveBufferSize);
        //_socket.UseOnlyOverlappedIO = true;
        _socket.Bind(remoteEndPoint);
        // join the multicast address
        _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastIp, IPAddress.Any));
        _socket.BeginReceiveFrom(_socketState.Buffer, 0, _socketState.BufferSize, SocketFlags.None, ref _socketState.RemoteEndPoint, AsyncReceiveCallback, _socketState);
    }

    /// <summary>
    /// Stop the server
    /// </summary>
    public void Stop()
    {
        _socket?.Close();
    }

    private void AsyncReceiveCallback(IAsyncResult ar)
    {
        // receive some data
        try
        {
            _readTimeoutTimer.Stop();
            var socketState = (SocketState)ar.AsyncState;
            var socket = socketState.Socket;
            var read = socket.EndReceiveFrom(ar, ref socketState.RemoteEndPoint);
            if (read > 0)
            {
                //Debug.WriteLine($"Received {read} bytes");
                // it is very important that ReadPacket responds quickly, or packets could be dropped.
                // process using the packetreader, frame events will be sent when parsed
                ProcessUdpMessage(new ArraySegment<byte>(socketState.Buffer, 0, read));
                if (_connectionLost)
                {
                    _connectionLost = false;
                    ConnectionRestored?.Invoke(this, new ConnectionRestoredEventArgs(Name, Uri));
                }
            }

            // start another receive
            if (!_isDisposed)
            {
                _readTimeoutTimer.Start();
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

    internal void ProcessUdpMessage(ArraySegment<byte> buffer)
    {
        _packetProcessor.ReadPacket(buffer);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (!isDisposing || _isDisposed)
            return;

        Stop();
        // let the socket settle before disposing it
        _socket?.Dispose();
        _packetProcessor.Dispose();
        _isDisposed = true;
    }
}
