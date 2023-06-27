using System.Diagnostics;
using System.Text;
using WallMonitor.Common.IO.Messages;
using WallMonitor.Common.IO.Security;

namespace WallMonitor.Common.IO
{
    public class ServerEventPacketProcessor : IDisposable
    {
        // 1.5MB ring buffer, 1500 MTU * 1000 packets
        private const int BufferStreamLength = 1500 * 1000;
        private readonly TimeSpan _processBufferInterval = TimeSpan.FromMilliseconds(100);
        private readonly Thread _ringBufferProcessingThread;
        private readonly ManualResetEvent _isClosing = new(false);
        private readonly RingBuffer _ringBuffer = new RingBuffer(BufferStreamLength);
        private bool _isDisposed = false;
        private readonly IAesEncryptionService _aesEncryptionService;
        private readonly string? _encryptionKey;

        /// <summary>
        /// Server update notification
        /// </summary>
        public event EventHandler<ServerNotificationEventArgs>? ServerUpdateNotificationReceived;

        /// <summary>
        /// Monitor service configuration
        /// </summary>
        public event EventHandler<MonitorConfigurationEventArgs>? ConfigurationReceived;

        public ServerEventPacketProcessor(string? encryptionKey, IAesEncryptionService aesEncryptionService)
        {
            _encryptionKey = encryptionKey;
            _aesEncryptionService = aesEncryptionService;
            _ringBufferProcessingThread = new Thread(ReceiveBufferProcessingThread)
            {
                Name = "Ring Buffer Processing Thread"
            };
            _ringBufferProcessingThread.Start();
        }

        public void ReadPacket(ArraySegment<byte> packetBytes)
        {
            if (packetBytes.Array == null || packetBytes.Count == 0)
                return;

            // store data in the ring buffer to be processed in a different thread.
            // we don't want to process the UDP buffer immediately as the protocol is very sensitive
            // to receive speed and we don't want to lose any data
            _ringBuffer.WriteBytes(packetBytes);
        }

        /// <summary>
        /// Process a monitor service configuration message
        /// </summary>
        /// <param name="messageVersion"></param>
        /// <param name="reader"></param>
        private void ProcessMonitorServiceConfiguration(byte messageVersion, BinaryReader reader)
        {
            var mapping = new Dictionary<byte, Action<BinaryReader>>
            {
                { MessageVersions.MonitorServiceConfiguration.Versions[0], ProcessMonitorServiceConfigurationV1 }
            };
            mapping[messageVersion].Invoke(reader);
        }

        private void ProcessMonitorServiceConfigurationV1(BinaryReader reader)
        {
            var configuration = new LimitedConfiguration();
            configuration.Monitor = reader.ReadString();
            var hostCount = reader.ReadUInt16();
            for (var i = 0; i < hostCount; i++)
            {
                var displayName = reader.ReadString();
                var hostName = reader.ReadString();
                var orderId = reader.ReadUInt16();
                var hostIsEnabled = reader.ReadBoolean();
                var hostImageTheme = reader.ReadByte();
                var hostImageSize = reader.ReadByte();
                var limitedHost = new LimitedHost
                {
                    Name = displayName,
                    HostName = hostName,
                    OrderId = orderId,
                    Enabled = hostIsEnabled,
                    ImageTheme = hostImageTheme,
                    ImageSize = hostImageSize
                };
                var serviceCount = reader.ReadUInt16();
                for (var s = 0; s < serviceCount; s++)
                {
                    var serviceName = reader.ReadString();
                    var serviceIsEnabled = reader.ReadBoolean();
                    limitedHost.Services.Add(new LimitedService { Name = serviceName, Enabled = serviceIsEnabled });
                }
                configuration.Hosts.Add(limitedHost);
            }

            ConfigurationReceived?.Invoke(this, new MonitorConfigurationEventArgs { Configuration = configuration });
        }

        /// <summary>
        /// Process a server status update message
        /// </summary>
        /// <param name="messageVersion"></param>
        /// <param name="reader"></param>
        private void ProcessServerStatusUpdate(byte messageVersion, BinaryReader reader)
        {
            var mapping = new Dictionary<byte, Action<BinaryReader>>
            {
                { MessageVersions.ServerStatusUpdate.Versions[0], ProcessServerStatusUpdateV1 }
            };
            mapping[messageVersion].Invoke(reader);
        }

        private void ProcessServerStatusUpdateV1(BinaryReader reader)
        {
            var e = new ServerNotificationEventArgs();
            // eventTypeByte, graphType...
            var eventTypeByte = reader.ReadByte();
            if (!Enum.IsDefined<EventType>((EventType)eventTypeByte))
                Debug.WriteLine("ERROR: Invalid response received!");
            // order matters!
            e.EventType = (EventType)eventTypeByte;
            var graphTypeByte = reader.ReadByte();
            e.GraphType = (GraphType)graphTypeByte;
            var hostLength = reader.ReadInt16();
            var hostBytes = reader.ReadBytes(hostLength);
            e.Host = Encoding.UTF8.GetString(hostBytes);
            var serviceLength = reader.ReadInt16();
            var serviceBytes = reader.ReadBytes(serviceLength);
            e.Service = Encoding.UTF8.GetString(serviceBytes);
            e.DateTime = new DateTime(reader.ReadInt64());
            e.ServiceState = (ServiceState)reader.ReadByte();
            e.Value = reader.ReadDouble();
            var rangeLength = reader.ReadInt16();
            var rangeBytes = reader.ReadBytes(rangeLength);
            e.Range = Encoding.UTF8.GetString(rangeBytes);
            e.Units = (Units)reader.ReadByte();
            e.ResponseTime = reader.ReadInt64();
            e.LastUpTime = new DateTime(reader.ReadInt64());
            e.PreviousDownTime = TimeSpan.FromTicks(reader.ReadInt64());
            ServerUpdateNotificationReceived?.Invoke(this, e);
        }

        private void ReceiveBufferProcessingThread()
        {
            while (!_isClosing.WaitOne(_processBufferInterval))
            {
                // if there is data in the ring buffer, read it and process
                if (_ringBuffer.Length > 0)
                {
                    var bytesToProcess = _ringBuffer.ReadAllBytes();
                    ProcessBuffer(bytesToProcess);
                }
            }
        }

        /// <summary>
        /// Process data in the buffer for IMessage messages
        /// </summary>
        /// <param name="buffer"></param>
        private void ProcessBuffer(ArraySegment<byte> buffer)
        {
            // read all the data in the buffer
            if (buffer.Array == null || buffer.Count == 0)
                return;

            using var stream = new MemoryStream(buffer.Array);
            using var reader = new BinaryReader(stream);
            var offset = 0;
            while (offset < buffer.Count)
            {
                // read the message header
                var messageVersion = reader.ReadByte();
                var messageTypeByte = reader.ReadByte();
                var encryptionTypeByte = reader.ReadByte();
                var messageType = (MessageTypes)messageTypeByte;
                var encryptionType = (EncryptionTypes)encryptionTypeByte;

                if (!Enum.IsDefined<MessageTypes>(messageType))
                    throw new DataMisalignedException($"Unknown message type received: '{messageType}'");

                if (!Enum.IsDefined<EncryptionTypes>(encryptionType))
                    throw new DataMisalignedException($"Unknown encryption type received: '{encryptionType}'");

                // todo: make this more generic
                switch (messageType)
                {
                    case MessageTypes.MonitorServiceConfiguration:
                        if (!MessageVersions.MonitorServiceConfiguration.Versions.Contains(messageVersion))
                            throw new DataMisalignedException($"Unknown message version {messageVersion} for message type: '{messageType}'");
                        break;
                    case MessageTypes.ServerStatusUpdate:
                        if (!MessageVersions.ServerStatusUpdate.Versions.Contains(messageVersion))
                            throw new DataMisalignedException($"Unknown message version {messageVersion} for message type: '{messageType}'");
                        break;
                }

                if (encryptionType != EncryptionTypes.Unencrypted && string.IsNullOrEmpty(_encryptionKey))
                    throw new InvalidOperationException("A message with encryption has been received however no encryption key was provided to decrypt it!");

                var messageLength = reader.ReadUInt16();
                if (messageLength > buffer.Count - offset)
                {
                    throw new DataMisalignedException($"Incorrect message length! Expect: {messageLength} Actual: {stream.Length} ");
                }

                var message = new Message { 
                    MessageVersion = messageVersion, 
                    MessageType = messageType, 
                    EncryptionType = encryptionType,
                    Length = messageLength
                };
                var bytesRead = ReadMessage(message, reader);
                
                // message length indicates the entire message size, including the first 3 bytes we read
                offset += messageLength;
            }
        }

        private byte[] DecryptMessage(IMessage message, BinaryReader reader, int length)
        {
            return Task.Run(async () => await _aesEncryptionService.DecryptAsync(reader.ReadBytes(length), _encryptionKey))
                .GetAwaiter()
                .GetResult();
        }

        private int ReadMessage(IMessage message, BinaryReader reader)
        {
            byte[] bytes;
            var bytesAlreadyRead = message.HeaderSize;
            
            switch (message.EncryptionType)
            {
                default:
                case EncryptionTypes.Unencrypted:
                    // do nothing
                    bytes = reader.ReadBytes(message.Length - bytesAlreadyRead);
                    break;
                case EncryptionTypes.Aes256:
                    // decrypt message
                    bytes = DecryptMessage(message, reader, message.Length - bytesAlreadyRead);
                    break;
            }

            // create a new stream to read the message data
            using var stream = new MemoryStream(bytes);
            using var messageReader = new BinaryReader(stream);
            // read a message by its version and message type
            switch (message.MessageType)
            {
                case MessageTypes.MonitorServiceConfiguration:
                    ProcessMonitorServiceConfiguration(message.MessageVersion, messageReader);
                    break;
                case MessageTypes.ServerStatusUpdate:
                    ProcessServerStatusUpdate(message.MessageVersion, messageReader);
                    break;
                default:
                    Debug.WriteLine($"ERROR: Unknown message type received: {message.MessageType}");
                    break;
            }

            return bytes.Length;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool isDisposing)
        {
            if (!isDisposing || _isDisposed)
                return;
            _isDisposed = true;
            _isClosing?.Set();
            _ringBuffer?.Dispose();
        }
    }
}
