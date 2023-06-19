using System.Diagnostics;
using System.Text;
using SystemMonitor.Common.IO.Messages;
using SystemMonitor.Common.IO.Security;
using SystemMonitor.Common.IO.Server;
using SystemMonitor.Common.IO.Server.Transport;

namespace SystemMonitor.Common.IO
{
    /// <summary>
    /// Broadcasts service state via Tcp
    /// </summary>
    public class TcpServerBroadcaster : TcpServer, IBroadcaster
    {
        private const int LengthBytePosition = 3;
        private int _totalConnections = 0;
        private readonly EncryptionTypes _encryptionType;
        private readonly IAesEncryptionService _aesStringEncryptionService;
        private readonly string? _encryptionKey;
        private readonly LimitedConfiguration _limitedConfiguration;
        private readonly TcpServerConfiguration _tcpConfiguration;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TcpServerBroadcaster(LimitedConfiguration limitedConfiguration, TcpServerConfiguration tcpServerConfiguration, EncryptionTypes encryptionType, string? encryptionKey, IAesEncryptionService aesStringEncryptionService) : base(tcpServerConfiguration)
        {
            _encryptionType = encryptionType;
            _limitedConfiguration = limitedConfiguration;
            _tcpConfiguration = tcpServerConfiguration;
            
            if (_encryptionType != EncryptionTypes.Unencrypted && string.IsNullOrEmpty(encryptionKey))
                throw new InvalidOperationException("Encryption has been specified however no encryption key was provided!");

            _encryptionKey = encryptionKey;
            _aesStringEncryptionService = aesStringEncryptionService;
            var cancellationToken = _cancellationTokenSource.Token;
            Task.Run(() => OpenAsync(cancellationToken));
        }

        protected override Task StartedAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("TcpServer started!");
            return base.StartedAsync(stoppingToken);
        }

        protected override Task ShutdownAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("TcpServer shutdown!");
            return base.ShutdownAsync(stoppingToken);
        }

        protected override async Task ConnectionCompleteAsync(Connection connection)
        {
            //_hasNoActiveConnectionEvent.Reset();
            Interlocked.Increment(ref _totalConnections);
            //_connections.Add(connection.ConnectionId, (connection, new ManualResetEvent(false)));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Connection from {connection.RemoteEndPoint.ToString()} connected!");
            Console.ForegroundColor = ConsoleColor.Gray;

            await SendConfigurationAsync(_limitedConfiguration);
        }

        protected override Task ConnectionClosedAsync(Connection connection)
        {
            Interlocked.Decrement(ref _totalConnections);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Disconnected client {connection.RemoteEndPoint}");
            Console.ForegroundColor = ConsoleColor.Gray;

            return Task.CompletedTask;
        }

        public async Task SendAsync(ServerStatusUpdate message)
        {
            using var stream = new MemoryStream();
            await using var writer = new BinaryWriter(stream);

            // order matters!
            // messageVersion, messageType, messageLength, data
            var messageVersion = MessageVersions.ServerStatusUpdate.Latest;
            writer.Write(messageVersion);
            var messageTypeByte = (byte)MessageTypes.ServerStatusUpdate;
            writer.Write(messageTypeByte);
            var encryptionTypeByte = (byte)_encryptionType;
            writer.Write(encryptionTypeByte);
            writer.Write((ushort)0); // msg length, we will write this at the end

            // write the message to a separate stream
            using var messageStream = new MemoryStream();
            await using var messageWriter = new BinaryWriter(messageStream);
            var eventTypeByte = (byte)message.EventType;
            messageWriter.Write(eventTypeByte);
            var graphTypeByte = (byte)message.GraphType;
            messageWriter.Write(graphTypeByte);
            var hostBytes = Encoding.UTF8.GetBytes(message.Host);
            messageWriter.Write((ushort)hostBytes.Length);
            messageWriter.Write(hostBytes);
            var serviceBytes = Encoding.UTF8.GetBytes(message.Service);
            messageWriter.Write((ushort)serviceBytes.Length);
            messageWriter.Write(serviceBytes);
            messageWriter.Write(message.DateTime.Ticks);
            messageWriter.Write((byte)message.ServiceState);
            messageWriter.Write(message.Value ?? 0d);
            var rangeBytes = Encoding.UTF8.GetBytes(message.Range ?? string.Empty);
            messageWriter.Write((ushort)rangeBytes.Length);
            messageWriter.Write(rangeBytes);
            messageWriter.Write((byte)message.Units);
            messageWriter.Write(message.ResponseTime.Ticks);
            messageWriter.Write(message.LastUpTime.Ticks);
            messageWriter.Write(message.PreviousDownTime.Ticks);

            // encrypt the message if configured
            switch (_encryptionType)
            {
                case EncryptionTypes.Unencrypted:
                    writer.Write(messageStream.ToArray());
                    break;
                case EncryptionTypes.Aes256:
                    var encryptedBytes = await EncryptMessageAsync(messageStream.ToArray());
                    writer.Write(encryptedBytes);
                    break;
            }

            // seek to the 4th byte, where length is stored
            writer.Seek(LengthBytePosition, SeekOrigin.Begin);
            writer.Write((ushort)stream.Length);

            Debug.WriteLine($"Sending message type {messageTypeByte} ({stream.Length} length)");
            var bytes = stream.ToArray();
            await SendAsync(bytes);
        }

        private async Task<int> SendAsync(ArraySegment<byte> bytes)
        {
            return await WriteAsync(bytes);
        }

        private int Send(ArraySegment<byte> bytes)
        {
            return Write(bytes);
        }

        private async Task SendConfigurationAsync(LimitedConfiguration message)
        {
            using var stream = new MemoryStream();
            await using var writer = new BinaryWriter(stream);

            // order matters!
            // messageVersion, messageType, encryptionType, messageLength, data
            var messageVersion = MessageVersions.MonitorServiceConfiguration.Latest;
            writer.Write(messageVersion);
            var messageTypeByte = (byte)MessageTypes.MonitorServiceConfiguration;
            writer.Write(messageTypeByte);
            var encryptionTypeByte = (byte)_encryptionType;
            writer.Write(encryptionTypeByte);
            writer.Write((ushort)0); // msg length, we will write this at the end

            // write the message to a separate stream
            using var messageStream = new MemoryStream();
            await using var messageWriter = new BinaryWriter(messageStream);
            messageWriter.Write(message.Monitor);
            messageWriter.Write((ushort)message.Hosts.Count);
            foreach (var host in message.Hosts)
            {
                messageWriter.Write(host.Name);
                messageWriter.Write(host.HostName ?? string.Empty);
                messageWriter.Write((ushort)host.OrderId);
                messageWriter.Write(host.Enabled);
                messageWriter.Write(host.ImageTheme);
                messageWriter.Write(host.ImageSize);
                messageWriter.Write((ushort)host.Services.Count);
                foreach (var service in host.Services)
                {
                    messageWriter.Write(service.Name);
                    messageWriter.Write(service.Enabled);
                }
            }

            // encrypt the message if configured
            var unencryptedBytes = messageStream.ToArray();
            switch (_encryptionType)
            {
                case EncryptionTypes.Unencrypted:
                    writer.Write(unencryptedBytes);
                    break;
                case EncryptionTypes.Aes256:
                    var encryptedBytes = Task.Run(async () => await EncryptMessageAsync(unencryptedBytes)).GetAwaiter().GetResult();
                    writer.Write(encryptedBytes);
                    break;
            }

            // seek to the 4th byte, where length is stored
            writer.Seek(LengthBytePosition, SeekOrigin.Begin);
            writer.Write((ushort)stream.Length);

            Debug.WriteLine($"Sending message type {messageTypeByte}");
            var bytes = stream.ToArray();
            await SendAsync(bytes);
        }

        private async Task<byte[]> EncryptMessageAsync(byte[] unencryptedBytes)
        {
            // encrypt the message portion of the stream
            return await _aesStringEncryptionService.EncryptAsync(unencryptedBytes, _encryptionKey!);
        }
    }
}
