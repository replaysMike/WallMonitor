using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SystemMonitor.Common.IO.Messages;
using SystemMonitor.Common.IO.Security;

namespace SystemMonitor.Common.IO
{
    /// <summary>
    /// Broadcasts service state via Udp Multicast
    /// </summary>
    public class UdpBroadcaster : IBroadcaster
    {
        public static readonly TimeSpan ConfigurationSyncInterval = TimeSpan.FromSeconds(10);

        public const int DefaultUdpSendBufferSize = 2048; // 2kb, default is 64K
        private const int LengthBytePosition = 3;
        private readonly EncryptionTypes _encryptionType;
        private readonly IAesEncryptionService _aesStringEncryptionService;
        private readonly string? _encryptionKey;
        private readonly Socket _socket;
        private readonly IPEndPoint _remoteEndpoint;
        private readonly UdpBroadcasterConfiguration _configuration;
        private readonly LimitedConfiguration _limitedConfiguration;
        private readonly System.Timers.Timer _broadcastTimer = new (ConfigurationSyncInterval);

        public UdpBroadcaster(UdpBroadcasterConfiguration configuration, LimitedConfiguration limitedConfiguration, EncryptionTypes encryptionType, string? encryptionKey, IAesEncryptionService aesStringEncryptionService)
        {
            _configuration = configuration;
            _limitedConfiguration = limitedConfiguration;
            _encryptionType = encryptionType;
            
            if (_encryptionType != EncryptionTypes.Unencrypted && string.IsNullOrEmpty(encryptionKey))
                throw new InvalidOperationException("Encryption has been specified however no encryption key was provided!");

            _encryptionKey = encryptionKey;
            _aesStringEncryptionService = aesStringEncryptionService;
            var multicastIp = IPAddress.Parse(configuration.Uri.Host);
            _remoteEndpoint = new IPEndPoint(multicastIp, configuration.Uri.Port);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, DefaultUdpSendBufferSize);

            _broadcastTimer.Elapsed += BroadcastTimer_Elapsed;
            _broadcastTimer.Start();

            // send configuration immediately
            SendConfiguration(_limitedConfiguration);
        }

        /// <summary>
        /// Send a server event message
        /// </summary>
        /// <param name="message"></param>
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

            //Debug.WriteLine($"Sending message type {messageTypeByte} ({stream.Length} length)");
            var bytes = stream.ToArray();
            await SendAsync(bytes);
        }

        private async Task<int> SendAsync(ArraySegment<byte> bytes)
        {
            return await _socket.SendToAsync(bytes, SocketFlags.None, _remoteEndpoint);
        }

        private int Send(ArraySegment<byte> bytes)
        {
            // Debug.WriteLine($"Sending {bytes.Count} bytes");
            return _socket.SendTo(bytes, SocketFlags.None, _remoteEndpoint);
        }

        private void BroadcastTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // sync configuration
            SendConfiguration(_limitedConfiguration);
        }

        private void SendConfiguration(LimitedConfiguration message)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

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
            using var messageWriter = new BinaryWriter(messageStream);
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

            //Debug.WriteLine($"Sending message type {messageTypeByte}");
            var bytes = stream.ToArray();
            Send(bytes);
        }

        private async Task<byte[]> EncryptMessageAsync(byte[] unencryptedBytes)
        {
            // encrypt the message portion of the stream
            return await _aesStringEncryptionService.EncryptAsync(unencryptedBytes, _encryptionKey!);
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}
