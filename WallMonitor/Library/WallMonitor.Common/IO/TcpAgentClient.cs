using System.Net;
using System.Net.Sockets;
using WallMonitor.Common.IO.AgentMessages;
using WallMonitor.Common.IO.Messages;
using WallMonitor.Common.IO.Security;

namespace WallMonitor.Common.IO
{
    /// <summary>
    /// Connects to a SystemMonitor agent and receives data
    /// </summary>
    public class TcpAgentClient : IDisposable
    {
        private Socket? _client;
        private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(5);

        public event EventHandler<ClientEventArgs>? ClientConnected;
        public event EventHandler<ClientEventArgs>? ClientDisconnected;
        public event EventHandler<HardwareEventMessageReceivedArgs>? OnFirstMessageReceived;
        public event EventHandler<HardwareEventMessageReceivedArgs>? EventMessageReceived;

        public string? HostName { get; set; }
        public string? IpAddress { get; set; }
        public int Port { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; } = new(0, 0);
        private readonly SemaphoreSlim _lock = new(1);
        private readonly ManualResetEvent _closingEvent = new(false);
        private Thread? _receiveThread;
        private readonly byte[] _buffer = new byte[4096];
        private int _messageId = 0;
        private readonly List<ServiceConfiguration> _monitors = new();
        private readonly EncryptionTypes _encryptionType;
        private readonly string? _encryptionKey;
        private readonly AesEncryptionService _aesEncryptionService = new ();

        private HardwareInformationMessage? _hardwareInformation;
        /// <summary>
        /// Get the last Hardware Information received
        /// </summary>
        public HardwareInformationMessage? HardwareInformation => _hardwareInformation;

        /// <summary>
        /// Returns true if connected
        /// </summary>
        public bool IsConnected => _client?.Connected ?? false;

        public TcpAgentClient(string? hostName, string? ipAddress, int port, EncryptionTypes encryptionType, string? encryptionKey,  List<ServiceConfiguration> monitors)
        {
            if (string.IsNullOrEmpty(hostName) && string.IsNullOrEmpty(ipAddress))
                throw new ArgumentNullException($"A {nameof(hostName)} or {nameof(ipAddress)} must be provided!");
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
            if (encryptionType != EncryptionTypes.Unencrypted && string.IsNullOrEmpty(encryptionKey))
                throw new InvalidOperationException("Encryption has been specified however no encryption key was provided!");

            HostName = hostName;
            IpAddress = ipAddress;
            Port = port;
            _encryptionType = encryptionType;
            _encryptionKey = encryptionKey;
            _monitors = monitors;
        }

        public async Task<HardwareInformationMessage?> TryGetHardwareInformationAsync()
        {
            Console.WriteLine("Opening HardwareInformation lock...");
            await _lock.WaitAsync(100);
            try
            {
                return _hardwareInformation;
            }
            finally
            {
                _lock.Release();
                Console.WriteLine("Closed HardwareInformation lock!");
            }
        }

        public HardwareInformationMessage? TryGetHardwareInformation()
        {
            Console.WriteLine("Opening HardwareInformation lock...");
            _lock.Wait(100);
            try
            {
                return _hardwareInformation;
            }
            finally
            {
                _lock.Release();
                Console.WriteLine("Closed HardwareInformation lock!");
            }
        }

        public async Task ConnectAsync()
        {
            IPAddress ip;
            if (string.IsNullOrEmpty(IpAddress))
            {
                ip = Util.GetIpFromHostname(new Uri(HostName));
                IpAddress = ip.ToString();
            }
            else
            {
                ip = IPAddress.Parse(IpAddress);
            }

            var ipEndPoint = new IPEndPoint(ip, Port);
            RemoteEndPoint = ipEndPoint;
            IpAddress = ip.ToString();
            _client = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            while (!_client.Connected)
            {
                try
                {
                    Console.WriteLine("Connecting...");
                    var startTime = DateTime.UtcNow;
                    var result = _client.BeginConnect(ipEndPoint.Address, ipEndPoint.Port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(ConnectionTimeout, true);
                    if (_client.Connected)
                    {
                        _client.EndConnect(result);

                        ClientConnected?.Invoke(this, new ClientEventArgs(ipEndPoint.Address.ToString()));
                        Console.WriteLine("Connected!");
                        _messageId = 0;

                        _receiveThread = new Thread(ReceiveThreadAsync);
                        _receiveThread.Start();
                    }
                    else
                    {
                        // failed to reconnect, retry
                        Console.WriteLine($"Failed to connect!");
                        var delayAmount = ConnectionTimeout - (DateTime.UtcNow - startTime);
                        if (delayAmount.TotalMilliseconds > 0)
                            await Task.Delay(delayAmount);
                    }
                }
                catch (Exception ex)
                {
                    // failed to connect, todo: reconnect
                    Console.WriteLine($"Exception on connect thrown: {ex.Message}");
                }
            }
            Console.WriteLine("ConnectAsync() exited!");
        }

        private async Task<byte[]> EncryptMessageAsync(byte[] unencryptedBytes)
        {
            // encrypt the message portion of the stream
            return await _aesEncryptionService.EncryptAsync(unencryptedBytes, _encryptionKey!);
        }

        private byte[] CreateAgentConfigurationMessage()
        {
            // create the configuration
            var agentConfiguration = new AgentConfigurationMessage();
            agentConfiguration.EncryptionType = _encryptionType;
            agentConfiguration.MonitorsLength = (byte)_monitors.Count;
            foreach (var monitor in _monitors)
            {
                var monitorConfig = new MonitorConfiguration();
                monitorConfig.MonitorId = (ushort)monitor.MonitorId;
                monitorConfig.Monitor = monitor.Monitor;
                monitorConfig.Schedule = monitor.Schedule;
                monitorConfig.ConfigurationLength = (byte)monitor.Configuration.Count;
                foreach (var kvp in monitor.Configuration)
                    monitorConfig.Configuration.Add(kvp.Key, kvp.Value.ToString());
                agentConfiguration.Monitors.Add(monitorConfig);
            }

            // generate the bytes
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(agentConfiguration.Header);
            writer.Write((byte)agentConfiguration.EncryptionType);
            writer.Write(0); // length

            using var messageStream = new MemoryStream();
            using var messageWriter = new BinaryWriter(messageStream);

            messageWriter.Write(agentConfiguration.MonitorsLength);
            foreach (var monitor in agentConfiguration.Monitors)
            {
                messageWriter.Write(monitor.MonitorId);
                messageWriter.Write(monitor.Monitor);
                messageWriter.Write(monitor.Schedule);
                messageWriter.Write(monitor.ConfigurationLength);
                foreach (var config in monitor.Configuration)
                {
                    messageWriter.Write(config.Key);
                    messageWriter.Write(config.Value);
                }
            }

            var unencryptedBytes = messageStream.ToArray();
            switch (agentConfiguration.EncryptionType)
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
            writer.Seek(3, SeekOrigin.Begin);
            writer.Write((ushort)stream.Length);

            return stream.ToArray();
        }

        private async void ReceiveThreadAsync()
        {
            var remoteEndPoint = IpAddress!;
            try
            {
                Console.WriteLine("ReceiveThreadAsync() start");

                // first we must send configuration so the agent can activate the monitors needed
                
                var configBytes = CreateAgentConfigurationMessage();
                _ = await _client.SendAsync(configBytes, SocketFlags.None);

                // start receiving data
                var beginReceiveBytes = new BeginEventsReceiveMessage(_encryptionType).ToBytes();
                _ = await _client.SendAsync(beginReceiveBytes, SocketFlags.None);

                while (!_closingEvent.WaitOne(50))
                {
                    var receiveLength = await _client.ReceiveAsync(_buffer, SocketFlags.None);
                    if (receiveLength > 0)
                    {
                        var messages = ParseMessages(_buffer, 0, receiveLength);
                        if (messages.Any())
                        {
                            var lastMessage = messages.Last();
                            switch (lastMessage)
                            {
                                case HardwareInformationMessage hardwareInformation:
                                    {
                                        await _lock.WaitAsync();
                                        try
                                        {
                                            if (_messageId == 0)
                                                OnFirstMessageReceived?.Invoke(this, new HardwareEventMessageReceivedArgs(hardwareInformation, remoteEndPoint));
                                            Interlocked.Increment(ref _messageId);
                                            _hardwareInformation = hardwareInformation;
                                            EventMessageReceived?.Invoke(this, new HardwareEventMessageReceivedArgs(hardwareInformation, remoteEndPoint));
                                        }
                                        finally
                                        {
                                            _lock.Release();
                                        }
                                        break;
                                    }
                            }
                        }
                    }

                    //var response = Encoding.UTF8.GetString(buffer, 0, received);
                    //Console.WriteLine($"CPU Usage: {message.Cpu * 100d:n2}%");
                }
            }
            catch (SocketException ex)
            {
                ClientDisconnected?.Invoke(this, new ClientEventArgs(remoteEndPoint));
                Console.WriteLine($"Socket exception, reconnecting...");
                _client?.Shutdown(SocketShutdown.Both);
                _client?.Dispose();
                ConnectAsync();
            }
            catch (Exception ex)
            {
                ClientDisconnected?.Invoke(this, new ClientEventArgs(remoteEndPoint));
                Console.WriteLine($"Exception thrown: {ex.Message}");
                _client?.Shutdown(SocketShutdown.Both);
                _client?.Dispose();
                ConnectAsync();
            }
            Console.WriteLine("ReceiveThreadAsync() end");
        }

        private List<IAgentMessage> ParseMessages(byte[] buffer, int index, int length)
        {
            var messages = new List<IAgentMessage>();
            try
            {
                using var stream = new MemoryStream(buffer);
                using var reader = new BinaryReader(stream);
                var bytesToRead = length;

                while (bytesToRead > 0)
                {
                    var messageType = reader.ReadUInt16();
                    switch (messageType)
                    {
                        case HardwareInformationMessage.ExpectedHeader:
                            var message = ParseHardwareInformationMessage(length, reader);
                            bytesToRead -= (int)message.Length;
                            messages.Add(message);
                            break;
                        default:
                            throw new Exception($"ERROR: Expected a hardware info message, but received: {messageType}");
                    }
                }

                return messages;
            }
            catch (Exception ex)
            {
                return messages;
            }
        }

        private HardwareInformationMessage ParseHardwareInformationMessage(int length, BinaryReader reader)
        {
            // parse hardware info message
            var message = new HardwareInformationMessage();
            var encryptionTypeByte = reader.ReadByte();
            var encryptionType = (EncryptionTypes)encryptionTypeByte;
            message.Length = reader.ReadUInt32();
            if (length < message.Length)
                throw new Exception($"Unexpected read length! Expected: {message.Length} but received {length}");

            // if info is encrypted, decrypt it
            var bytesToRead = (int)(message.Length - sizeof(ushort) - sizeof(byte) - sizeof(uint));
            var messageBytes = reader.ReadBytes(bytesToRead);
            byte[] decryptedBytes;
            switch (encryptionType)
            {
                default:
                case EncryptionTypes.Unencrypted:
                    decryptedBytes = messageBytes;
                    break;
                case EncryptionTypes.Aes256:
                    // decrypt bytes
                    decryptedBytes = Task.Run(async () => await _aesEncryptionService.DecryptAsync(messageBytes, _encryptionKey))
                        .GetAwaiter()
                        .GetResult();
                    break;
            }

            using var messageStream = new MemoryStream(decryptedBytes);
            using var messageReader = new BinaryReader(messageStream);

            message.Cpu = messageReader.ReadDouble();
            message.TotalMemoryInstalled = messageReader.ReadUInt64();
            message.TotalMemoryAvailable = messageReader.ReadUInt64();

            // read the drive names
            message.NumberOfDrives = messageReader.ReadByte();
            for (var i = 0; i < message.NumberOfDrives; i++)
            {
                var driveId = messageReader.ReadByte();
                var driveName = messageReader.ReadString();
                message.Drives.Add(driveId, driveName);
            }

            // read the drive size totals
            for (var i = 0; i < message.NumberOfDrives; i++)
            {
                var driveId = messageReader.ReadByte();
                var driveSize = messageReader.ReadUInt64();
                message.DriveSpaceTotal.Add(driveId, driveSize);
            }

            // read the drive space available
            for (var i = 0; i < message.NumberOfDrives; i++)
            {
                var driveId = messageReader.ReadByte();
                var driveSpaceAvailable = messageReader.ReadUInt64();
                message.DriveSpaceAvailable.Add(driveId, driveSpaceAvailable);
            }

            // read the monitors and their responses
            message.NumberOfMonitors = messageReader.ReadByte();
            for (var i = 0; i < message.NumberOfMonitors; i++)
            {
                var hostId = messageReader.ReadInt32();
                var isUp = messageReader.ReadBoolean();
                var value = messageReader.ReadDouble();
                var responseTime = messageReader.ReadInt64();
                message.Monitors.Add(new ServiceInfo
                {
                    MonitorId = hostId,
                    IsUp = isUp,
                    Value = value,
                    ResponseTime = responseTime
                });
            }

            return message;
        }

        public void Dispose()
        {
            _closingEvent.Set();
            _client?.Shutdown(SocketShutdown.Both);
            _client?.Dispose();
            _lock.Dispose();
        }
    }
}
