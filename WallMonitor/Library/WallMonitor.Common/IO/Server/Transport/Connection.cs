using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace WallMonitor.Common.IO.Server.Transport
{
    //From: https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketConnection.cs
    public class Connection : IAsyncDisposable
    {
        private const int MinBuffSize = 1024;
        private readonly Socket _socket;
        private readonly Receiver _receiver;
        private Sender? _sender;
        private readonly SenderPool _senderPool;
        private Task? _receiveTask;
        private Task? _sendTask;
        private readonly Pipe _transportPipe;
        private readonly Pipe _applicationPipe;
        private readonly object _shutdownLock = new object();
        private volatile bool _socketDisposed;
        public PipeWriter Output { get; }
        public PipeReader Input { get; }
        public EndPoint RemoteEndPoint => _socket.RemoteEndPoint;
        public event EventHandler<ConnectionEventArgs>? OnDisconnect;
        public Guid ConnectionId { get; }

        public Connection(Socket socket, SenderPool senderPool, PipeScheduler transportScheduler, PipeScheduler applicationScheduler, MemoryPool<byte> memoryPool)
        {
            ConnectionId = Guid.NewGuid();
            _socket = socket;
            _receiver = new Receiver();
            _senderPool = senderPool;
            _transportPipe = new Pipe(new PipeOptions(memoryPool, applicationScheduler, transportScheduler, useSynchronizationContext: false));
            Output = _transportPipe.Writer;
            _applicationPipe = new Pipe(new PipeOptions(memoryPool, transportScheduler, applicationScheduler, useSynchronizationContext: false));
            Input = _applicationPipe.Reader;
        }

        public void Start()
        {
            try
            {
                _sendTask = SendLoop();
                _receiveTask = ReceiveLoop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task SendLoop()
        {
            try
            {
                while (true)
                {
                    var result = await _transportPipe.Reader.ReadAsync();
                    var buff = result.Buffer;
                    if (!buff.IsEmpty)
                    {
                        _sender = _senderPool.Rent();
                        await _sender.SendAsync(_socket, result.Buffer);
                        _senderPool.Return(_sender);
                        _sender = null;
                    }

                    _transportPipe.Reader.AdvanceTo(buff.End);
                    if (result.IsCompleted || result.IsCanceled)
                    {
                        break;
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                // connection disconnected, socket was disposed
                /*Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Gray;*/
            }
            catch (SocketException ex)
            {
                // connection disconnected, socket was disposed
                /*Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Gray;*/
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            finally
            {
                var remoteEndPoint = RemoteEndPoint;
                try
                {
                    await _applicationPipe.Writer.CompleteAsync();
                    Shutdown();
                }
                catch (Exception)
                {
                    // ignore all shutdown errors
                }
                OnDisconnect?.Invoke(this, new ConnectionEventArgs(remoteEndPoint, ConnectionId));
            }
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (true)
                {
                    var buff = _applicationPipe.Writer.GetMemory(MinBuffSize);
                    var bytes = await _receiver.ReceiveAsync(_socket, buff);
                    if (bytes <= 0)
                    {
                        break;
                    }

                    _applicationPipe.Writer.Advance(bytes);
                    var result = await _applicationPipe.Writer.FlushAsync();
                    if (result.IsCanceled || result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                // connection disconnected, socket was disposed
                /*Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Gray;*/
            }
            catch (SocketException ex)
            {
                // connection disconnected, socket was disposed
                /*Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Gray;*/
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            finally
            {
                var remoteEndPoint = RemoteEndPoint;
                try
                {
                    await _applicationPipe.Writer.CompleteAsync();
                    Shutdown();
                }
                catch (Exception)
                {
                    // ignore all shutdown errors
                }
                OnDisconnect?.Invoke(this, new ConnectionEventArgs(remoteEndPoint, ConnectionId));
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _transportPipe.Reader.CompleteAsync();
            await _applicationPipe.Writer.CompleteAsync();
            try
            {
                if (_receiveTask != null)
                {
                    await _receiveTask;
                }

                if (_sendTask != null)
                {
                    await _sendTask;
                }
            }
            finally
            {
                _receiver.Dispose();
                _sender?.Dispose();
            }
        }
        public void Shutdown()
        {
            lock (_shutdownLock)
            {
                if (_socketDisposed)
                {
                    return;
                }
                _socketDisposed = true;
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    _socket.Dispose();
                }
            }
        }
    }
}
