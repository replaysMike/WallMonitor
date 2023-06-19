using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using SystemMonitor.Common.IO.AgentMessages;
using SystemMonitor.Common.IO.Server.MemoryPool;
using SystemMonitor.Common.IO.Server.Transport;

namespace SystemMonitor.Common.IO.Server
{
    /// <summary>
    /// Host a Tcp Server
    /// </summary>
    public class TcpServer : IDisposable
    {
        public event EventHandler<ConnectionEventArgs>? OnConnect;
        public event EventHandler<ConnectionEventArgs>? OnDisconnect;
        public event EventHandler<ConnectionRejectedEventArgs>? OnConnectionRejected;

        private readonly TcpServerConfiguration _configuration;
        private Socket? _listenSocket;
        private SenderPool? _senderPool;
        private IOQueue? _transportScheduler;
        private PinnedBlockMemoryPool? _memoryPool;
        private PipeScheduler? _applicationScheduler;
        private readonly AutoResetEvent _sendEvent = new(false);
        private int _totalConnections;
        private readonly SemaphoreSlim _dataLock = new (1, 1);
        private readonly List<Connection> _activeConnections = new List<Connection>();

        public TcpServer(TcpServerConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Open the Tcp Server and start waiting for connections
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task OpenAsync(CancellationToken cancellationToken)
        {
            // listen for connections
            IPEndPoint ipEndPoint;
            if (_configuration.Uri.OriginalString.Equals("127.0.0.1"))
                ipEndPoint= new IPEndPoint(IPAddress.Any, _configuration.Uri.Port);
            else
                ipEndPoint= new IPEndPoint(IPAddress.Parse(_configuration.Uri.Host), _configuration.Uri.Port);
            _listenSocket = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _listenSocket.Bind(ipEndPoint);
            _listenSocket.Listen(32);

            _transportScheduler = new IOQueue();
            _applicationScheduler = PipeScheduler.ThreadPool;
            _senderPool = new SenderPool();
            _memoryPool = new PinnedBlockMemoryPool();

            // start accepting connections, block until cancellation token is cancelled
            await AcceptConnectionsAsync(cancellationToken);
        }

        private async Task AcceptConnectionsAsync(CancellationToken stoppingToken)
        {
            if (_listenSocket == null || _senderPool == null || _transportScheduler == null || _applicationScheduler == null || _memoryPool == null)
                return;

            try
            {
                await StartedAsync(stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    var socket = await _listenSocket.AcceptAsync(stoppingToken);
                    if (_configuration.AllowFrom.Any() && !IpAddressTools.IsIpAddressAllowed(((IPEndPoint)socket.RemoteEndPoint).Address, _configuration.AllowFrom, true))
                    {
                        OnConnectionRejected?.Invoke(this, new ConnectionRejectedEventArgs(socket));
                        await socket.DisconnectAsync(true, stoppingToken);
                        return;
                    }

                    var connection = new Connection(socket, _senderPool, _transportScheduler, _applicationScheduler, _memoryPool);
                    await _dataLock.WaitAsync(stoppingToken);
                    try
                    {
                        _activeConnections.Add(connection);
                    }
                    finally
                    {
                        _dataLock.Release();
                    }

                    OnConnect?.Invoke(this, new ConnectionEventArgs(socket.RemoteEndPoint, connection.ConnectionId));
                    connection.OnDisconnect += Connection_OnDisconnect;
                    // don't wait, process I/O in a task and wait for more connections
                    _ = ProcessConnection(connection);
                }
            }
            catch (SocketException ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"AcceptConnectionsAsync exception: {ex.GetBaseException().Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                throw;
            }
            await ShutdownAsync(stoppingToken);
        }

        private async Task ProcessConnection(Connection connection)
        {
            try
            {
                connection.Start();
                Interlocked.Increment(ref _totalConnections);
                await ConnectionCompleteAsync(connection);

                while (true)
                {
                    var result = await connection.Input.ReadAsync();
                    var buff = result.Buffer;
                    var continueReading = false;
                    try
                    {
                        continueReading = await ReadPacketAsync(connection, result);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"WARN: Exception thrown in ReadPacketAsync: {ex.GetBaseException().Message}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    connection.Input.AdvanceTo(buff.End);
                    if (result.IsCompleted || result.IsCanceled || !continueReading)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception: {ex.GetBaseException().Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            finally
            {
                connection.Shutdown();
                await connection.DisposeAsync();
                await ConnectionClosedAsync(connection);
                await _dataLock.WaitAsync();
                try
                {
                    _activeConnections.RemoveAll(x => x.ConnectionId == connection.ConnectionId);
                }
                finally
                {
                    _dataLock.Release();
                }

                Interlocked.Decrement(ref _totalConnections);
            }
        }

        protected virtual Task StartedAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task ShutdownAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task ConnectionCompleteAsync(Connection connection)
        {
            return Task.CompletedTask;
        }

        protected virtual Task ConnectionClosedAsync(Connection connection)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Read an incoming packet of data
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="readResult">A sequence of bytes that were read from the socket</param>
        /// <returns>Return true to continue reading. If false is returned, the connection will be closed.</returns>
        protected virtual Task<bool> ReadPacketAsync(Connection connection, ReadResult readResult)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Write data to all connections
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected virtual async Task<int> WriteAsync(ArraySegment<byte> bytes)
        {
            await _dataLock.WaitAsync();
            try
            {
                foreach (var connection in _activeConnections)
                {
                    await connection.Output.WriteAsync(bytes);
                }
            }
            finally
            {
                _dataLock.Release();
            }

            return bytes.Array?.Length ?? 0;
        }

        /// <summary>
        /// Write data to all connections
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected virtual int Write(ArraySegment<byte> bytes)
        {
            _dataLock.Wait();
            try
            {
                foreach (var connection in _activeConnections)
                {
                    connection.Output.Write(bytes);
                }
            }
            finally
            {
                _dataLock.Release();
            }

            return bytes.Array?.Length ?? 0;
        }

        /// <summary>
        /// Write data to a single connection
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected virtual async Task<int> WriteAsync(Guid connectionId, ArraySegment<byte> bytes)
        {
            await _dataLock.WaitAsync();
            try
            {
                var connection = _activeConnections.FirstOrDefault(x => x.ConnectionId == connectionId);
                if (connection != null)
                {
                    await connection.Output.WriteAsync(bytes);
                }
            }
            finally
            {
                _dataLock.Release();
            }

            return bytes.Array?.Length ?? 0;
        }

        /// <summary>
        /// Write data to a single connection
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected virtual int Write(Guid connectionId, ArraySegment<byte> bytes)
        {
            _dataLock.Wait();
            try
            {
                var connection = _activeConnections.FirstOrDefault(x => x.ConnectionId == connectionId);
                if (connection != null)
                {
                    connection.Output.Write(bytes);
                }
            }
            finally
            {
                _dataLock.Release();
            }

            return bytes.Array?.Length ?? 0;
        }

        private void Connection_OnDisconnect(object? sender, ConnectionEventArgs e)
        {
            OnDisconnect?.Invoke(this, new ConnectionEventArgs(e.RemoteEndPoint, e.ConnectionId));
        }

        public virtual void Dispose()
        {
            DisposeInternalResources();
        }

        private void DisposeInternalResources()
        {
            _sendEvent.Dispose();
            _listenSocket?.Dispose();
            _senderPool?.Dispose();
            _memoryPool?.Dispose();
            _dataLock.Dispose();
        }
    }
}
