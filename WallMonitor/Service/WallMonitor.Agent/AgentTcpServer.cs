using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using WallMonitor.Common.Abstract;
using WallMonitor.Common.IO.AgentMessages;
using WallMonitor.Common.IO.Messages;
using WallMonitor.Common.IO.Security;
using WallMonitor.Common.IO.Server;
using WallMonitor.Common.IO.Server.Transport;
using WallMonitor.Common.Models;
using ThreadState = System.Threading.ThreadState;

namespace WallMonitor.Agent
{
    public class AgentTcpServer : TcpServer
    {
        private static readonly TimeSpan BroadcastEvery = TimeSpan.FromMilliseconds(500);
        private readonly Configuration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly HardwareInformationMessage _hardwareInformation = new();
        private readonly AutoResetEvent _sendEvent = new(false);
        private readonly Dictionary<Guid, (Connection, ManualResetEvent)> _connections = new();
        private readonly Dictionary<Guid, List<MonitorConfiguration>> _monitorConfigurations = new();
        private readonly Dictionary<Guid, HashSet<ISchedule>> _schedules = new();
        // Max number of threads, or monitors, that can be run at once (only for non-asynchronous monitors)
        private readonly int _maxThreads;
        private readonly Dictionary<Guid, CancellationTokenSource> _cancellationTokens = new();
        private int _monitorThreadsRunning = 0;
        private int _dispatchThreadsRunning = 0;
        private int _totalConnections = 0;
        private Thread? _schedulingThread;
        private readonly ManualResetEvent _hasNoActiveConnectionEvent = new(false);
        private readonly object _cancelTokensLock = new();
        private readonly object _dataLock = new();
        private readonly SemaphoreSlim _hardwareInformationLock = new SemaphoreSlim(1, 1);
        private readonly IAesEncryptionService? _aesEncryptionService;

        public AgentTcpServer(Configuration configuration, IServiceProvider serviceProvider) : base(new TcpServerConfiguration
        {
            Uri = new Uri($"tcp://{(configuration.Ip == "*" ? "127.0.0.1" : configuration.Ip)}:{configuration.Port}"),
            AllowFrom = configuration.AllowFrom
        })
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _maxThreads = configuration.MaxThreads;
            OnConnectionRejected += AgentTcpServer_OnConnectionRejected;
            if (!string.IsNullOrEmpty(_configuration.EncryptionKey))
            {
                _aesEncryptionService = serviceProvider.GetRequiredService<IAesEncryptionService>();
                _hardwareInformation.EncryptionType = EncryptionTypes.Aes256;
            }
        }

        private void AgentTcpServer_OnConnectionRejected(object? sender, ConnectionRejectedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Connection from '{e.Socket.RemoteEndPoint}' not allowed.");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void SetHardwareInformation(HardwareInformationMessage hardwareInformation)
        {
            _hardwareInformationLock.Wait();
            try
            {
                _hardwareInformation.Cpu = hardwareInformation.Cpu;
                _hardwareInformation.TotalMemoryInstalled = hardwareInformation.TotalMemoryInstalled;
                _hardwareInformation.TotalMemoryAvailable = hardwareInformation.TotalMemoryAvailable;
                _hardwareInformation.NumberOfDrives = hardwareInformation.NumberOfDrives;
                _hardwareInformation.Drives = hardwareInformation.Drives;
                _hardwareInformation.DriveSpaceTotal = hardwareInformation.DriveSpaceTotal;
                _hardwareInformation.DriveSpaceAvailable = hardwareInformation.DriveSpaceAvailable;
            }
            finally
            {
                _hardwareInformationLock.Release();
            }

            _sendEvent.Set();
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

        protected override Task ConnectionCompleteAsync(Connection connection)
        {
            _hasNoActiveConnectionEvent.Reset();
            Interlocked.Increment(ref _totalConnections);
            _connections.Add(connection.ConnectionId, (connection, new ManualResetEvent(false)));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Connection from {connection.RemoteEndPoint.ToString()} connected!");
            Console.ForegroundColor = ConsoleColor.Gray;
            return Task.CompletedTask;
        }

        protected override Task ConnectionClosedAsync(Connection connection)
        {
            Interlocked.Decrement(ref _totalConnections);
            if (_totalConnections == 0)
                _hasNoActiveConnectionEvent.Set();

            if (Monitor.TryEnter(_dataLock))
            {
                try
                {
                    TryRemoveConnection(connection.ConnectionId);
                }
                finally
                {
                    Monitor.Exit(_dataLock);
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Disconnected client {connection.RemoteEndPoint}");
            Console.ForegroundColor = ConsoleColor.Gray;

            return Task.CompletedTask;
        }

        protected override async Task<bool> ReadPacketAsync(Connection connection, ReadResult readResult)
        {
            var buff = readResult.Buffer;
            if (buff.IsSingleSegment)
            {
                // read in the agent configuration
                using var memoryStream = new MemoryStream(buff.ToArray());
                using var reader = new BinaryReader(memoryStream);
                while (memoryStream.Length - memoryStream.Position > 0)
                {
                    var messageType = reader.ReadUInt16();
                    if (messageType == AgentConfigurationMessage.ExpectedHeader)
                    {
                        var encryptionTypeByte = reader.ReadByte();
                        var encryptionType = (EncryptionTypes)encryptionTypeByte;
                        // read the agent configuration
                        var monitors = ReadAgentConfiguration(reader, encryptionType);
                        try
                        {
                            if (Monitor.TryEnter(_dataLock))
                            {
                                _monitorConfigurations.Add(connection.ConnectionId, monitors);
                                var schedules = CreateSchedules(connection.ConnectionId, monitors);
                                _schedules.Add(connection.ConnectionId, schedules);
                            }
                        }
                        finally
                        {
                            Monitor.Exit(_dataLock);
                        }

                        LaunchSchedulingThreadIfNotRunning();
                    }
                    else if (messageType == BeginEventsReceiveMessage.ExpectedHeader)
                    {
                        // start sending of data
                        await SendHardwareEventStreamAsync(connection, _sendEvent);
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Buffer type of multiple segment not supported!");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            // always return true, otherwise the connection will be closed.
            return true;
        }

        private void LaunchSchedulingThreadIfNotRunning()
        {
            // launch a thread which will handle scheduling of monitors
            if (_schedulingThread == null || _schedulingThread.ThreadState != ThreadState.Running)
            {
                if (_schedulingThread != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Scheduling thread not running {_schedulingThread.ThreadState}, {_schedulingThread.ManagedThreadId}, {_schedulingThread.IsAlive}, creating!");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Scheduling thread created!");
                }

                Console.ForegroundColor = ConsoleColor.Gray;
                _schedulingThread = new Thread(SchedulingThread);
                _schedulingThread.Start();
            }
        }

        private List<MonitorConfiguration> ReadAgentConfiguration(BinaryReader reader, EncryptionTypes encryptionType)
        {
            // read in the agent configuration so we know what monitors to activate
            var agentConfig = new AgentConfigurationMessage();
            var startPosition = reader.BaseStream.Position; // we read the 3 byte header already
            agentConfig.Length = reader.ReadUInt32(); // expected message length

            // handle decryption
            var dataAlreadyRead = sizeof(ushort) + sizeof(byte) + sizeof(uint);
            var remainderLength = (int)agentConfig.Length - dataAlreadyRead;
            var messageBytes = reader.ReadBytes(remainderLength);
            byte[] decryptedBytes;
            switch (encryptionType)
            {
                default:
                case EncryptionTypes.Unencrypted:
                    decryptedBytes = messageBytes;
                    break;
                case EncryptionTypes.Aes256:
                    // decrypt bytes
                    if (_aesEncryptionService == null || string.IsNullOrEmpty(_configuration.EncryptionKey))
                        throw new InvalidOperationException("Received encrypted message but encryption is not configured!");
                    decryptedBytes = Task.Run(async () => await _aesEncryptionService.DecryptAsync(messageBytes, _configuration.EncryptionKey))
                        .GetAwaiter()
                        .GetResult();
                    break;
            }

            using var messageStream = new MemoryStream(decryptedBytes);
            using var messageReader = new BinaryReader(messageStream);

            agentConfig.MonitorsLength = messageReader.ReadByte();
            var monitors = new List<MonitorConfiguration>();

            for (var i = 0; i < agentConfig.MonitorsLength; i++)
            {
                var monitorConfig = new MonitorConfiguration();
                monitorConfig.MonitorId = messageReader.ReadUInt16();
                monitorConfig.Monitor = messageReader.ReadString();
                monitorConfig.Schedule = messageReader.ReadString();

                // read in each monitor's configuration
                monitorConfig.ConfigurationLength = messageReader.ReadByte();
                for (var c = 0; c < monitorConfig.ConfigurationLength; c++)
                {
                    var key = messageReader.ReadString();
                    var value = messageReader.ReadString();
                    monitorConfig.Configuration.Add(key, value);
                }
                monitors.Add(monitorConfig);
            }

            return monitors;
        }

        private bool TryRemoveConnection(Guid connectionId)
        {
            if (_schedules.ContainsKey(connectionId))
                _schedules.Remove(connectionId);

            if (_monitorConfigurations.ContainsKey(connectionId))
                _monitorConfigurations.Remove(connectionId);

            if (_connections.ContainsKey(connectionId))
            {
                var connectionGroup = _connections.First(x => x.Key.Equals(connectionId));
                var closeEvent = connectionGroup.Value.Item2;
                closeEvent.Set();

                _connections.Remove(connectionId);
                return true;
            }
            return false;
        }

        private async Task SendHardwareEventStreamAsync(Connection connection, AutoResetEvent sendEvent)
        {
            var key = connection.ConnectionId;
            KeyValuePair<Guid, (Connection, ManualResetEvent)>? connectionGroup = null;
            try
            {
                if (Monitor.TryEnter(_dataLock))
                {
                    connectionGroup = _connections.FirstOrDefault(x => x.Key.Equals(key));
                    if (connectionGroup == null) throw new InvalidOperationException($"Connection '{key}' was not found!");
                }
            }
            finally
            {
                Monitor.Exit(_dataLock);
            }

            var closeEvent = connectionGroup.Value.Value.Item2;
            using var stream = new MemoryStream();
            await using var writer = new BinaryWriter(stream);
            while (!closeEvent.WaitOne(100))
            {
                if (sendEvent.WaitOne(BroadcastEvery))
                {
                    stream.Position = 0;
                    stream.SetLength(0);
                    await _hardwareInformationLock.WaitAsync();
                    try
                    {
                        _hardwareInformation.Length = _hardwareInformation.ComputeLength();
                        _hardwareInformation.NumberOfMonitors = (byte)_hardwareInformation.Monitors.Count;

                        writer.Write(_hardwareInformation.Header);
                        writer.Write((byte)_hardwareInformation.EncryptionType);

                        using var messageStream = new MemoryStream();
                        await using var messageWriter = new BinaryWriter(messageStream);

                        messageWriter.Write(_hardwareInformation.Cpu);
                        messageWriter.Write(_hardwareInformation.TotalMemoryInstalled);
                        messageWriter.Write(_hardwareInformation.TotalMemoryAvailable);
                        messageWriter.Write(_hardwareInformation.NumberOfDrives);
                        foreach (var drive in _hardwareInformation.Drives)
                        {
                            messageWriter.Write(drive.Key);
                            messageWriter.Write(drive.Value);
                        }

                        foreach (var drive in _hardwareInformation.DriveSpaceTotal)
                        {
                            messageWriter.Write(drive.Key);
                            messageWriter.Write(drive.Value);
                        }

                        foreach (var drive in _hardwareInformation.DriveSpaceAvailable)
                        {
                            messageWriter.Write(drive.Key);
                            messageWriter.Write(drive.Value);
                        }

                        messageWriter.Write(_hardwareInformation.NumberOfMonitors);
                        foreach (var monitor in _hardwareInformation.Monitors)
                        {
                            messageWriter.Write(monitor.MonitorId);
                            messageWriter.Write(monitor.IsUp);
                            messageWriter.Write(monitor.Value ?? 0d);
                            messageWriter.Write(monitor.ResponseTime);
                        }

                        byte[] messageBytes;
                        if (!string.IsNullOrEmpty(_configuration.EncryptionKey))
                        {
                            // encrypt the message
                            var unencryptedBytes = messageStream.ToArray();
                            messageBytes = await _aesEncryptionService.EncryptAsync(unencryptedBytes, _configuration.EncryptionKey);
                        }
                        else
                        {
                            messageBytes = messageStream.ToArray();
                        }

                        // write the message length
                        writer.Write(messageBytes.Length + sizeof(ushort) + sizeof(byte) + sizeof(uint));
                        // write the message data
                        writer.Write(messageBytes);
                    }
                    finally
                    {
                        _hardwareInformationLock.Release();
                    }

                    // var data to send
                    var bytes = stream.ToArray();
                    await connection.Output.WriteAsync(bytes);
                }
            }

            // connection closed
            closeEvent.Dispose();

            if (Monitor.TryEnter(_dataLock))
            {
                try
                {
                    TryRemoveConnection(key);
                }
                finally
                {
                    Monitor.Exit(_dataLock);
                }
            }

            Debug.WriteLine($"Disposed and removed connection {key}.");
        }

        private void SchedulingThread()
        {
            ulong iteration = 0;
            Interlocked.Increment(ref _dispatchThreadsRunning);
            while (!_hasNoActiveConnectionEvent.WaitOne(100))
            {
                iteration++;
                if (iteration >= ulong.MaxValue)
                    iteration = 0;

                var activeSchedulesCopy = new Dictionary<Guid, ISchedule[]>();
                // create a copy of the schedules to iterate, which keeps the lock open for less time.
                var isLocked = Monitor.TryEnter(_dataLock);
                if (isLocked)
                {
                    try
                    {
                        foreach (var schedule in _schedules)
                        {
                            var copy = new ISchedule[schedule.Value.Count];
                            schedule.Value.CopyTo(copy);
                            activeSchedulesCopy.Add(schedule.Key, copy);
                        }
                    }
                    finally
                    {
                        Monitor.Exit(_dataLock);
                    }
                }

                foreach (var schedule in activeSchedulesCopy)
                    ScheduleAndLaunchMonitors(schedule.Key, schedule.Value);
            }
            Interlocked.Decrement(ref _dispatchThreadsRunning);
        }

        /// <summary>
		/// An individual task to run a monitor and return its response.
		/// </summary>
		/// <param name="schedule"></param>
		/// <param name="cancelToken"></param>
		private async Task LaunchMonitorAsync(ISchedule schedule, CancellationTokenSource cancelToken)
        {
            try
            {
                // OnHostCheckStarted?.Invoke(this, new HostEventArgs(schedule));
                Interlocked.Increment(ref _monitorThreadsRunning);
                var response = await schedule.MonitorAsync.CheckHostAsync(schedule.Host, schedule.ConfigurationParameters, cancelToken.Token);
                schedule.LastRuntime = DateTime.UtcNow;
                await _hardwareInformationLock.WaitAsync();
                try
                {
                    var m = _hardwareInformation.Monitors.FirstOrDefault(x => x.MonitorId == schedule.MonitorId);
                    if (m == null)
                    {
                        m = new ServiceInfo
                        {
                            MonitorId = schedule.MonitorId,
                        };
                        _hardwareInformation.Monitors.Add(m);
                    }

                    m.IsUp = response.IsUp;
                    m.ResponseTime = response.ResponseTime.Ticks;
                    m.Value = response.Value;
                }
                finally
                {
                    _hardwareInformationLock.Release();
                }
                schedule.LastResponse = response;
                schedule.Value = response.Value;
                schedule.Range = response.Range;
                schedule.Units = response.Units;
                schedule.ResponseTime = response.ResponseTime;

                if (!response.IsUp)
                {
                    schedule.ConsecutiveAttempts++;
                    if (schedule.ConsecutiveAttempts >= schedule.Attempts)
                    {
                        // OnHostCheckFailed?.Invoke(this, new HostEventArgs(schedule));
                    }
                }
                else if (schedule.LastResponse != null && !schedule.LastResponse.IsUp)
                {
                    // OnHostCheckRecovered?.Invoke(this, new HostEventArgs(schedule));
                }

                if (response.IsUp)
                    schedule.ConsecutiveAttempts = 0;

                // OnHostCheckCompleted?.Invoke(this, new HostResponseEventArgs(schedule, response));
            }
            catch (Exception ex)
            {
                // Log exception
            }
            finally
            {
                RemoveCancellationToken(schedule.Id);
                Interlocked.Decrement(ref _monitorThreadsRunning);
            }
        }

        private void ScheduleAndLaunchMonitors(Guid connectionId, ISchedule[] activeSchedulesCopy, Func<ISchedule, bool>? monitorsToRun = null)
        {
            if (activeSchedulesCopy != null)
            {
                var query = monitorsToRun != null ? activeSchedulesCopy.Where(monitorsToRun) : activeSchedulesCopy;
                foreach (var schedule in query)
                {
                    // Launch a scheduled check, if we haven't exceeded the number of currently running threads
                    if (monitorsToRun != null || IsScheduled(schedule) && _monitorThreadsRunning < _maxThreads)
                    {
                        var cancelToken = new CancellationTokenSource();
                        lock (_cancelTokensLock)
                        {
                            // if a check of this schedule is still pending, don't re-task a new one.
                            if (_cancellationTokens.ContainsKey(schedule.Id))
                                continue;
                            _cancellationTokens.Add(schedule.Id, cancelToken);
                        }

                        // run monitor but don't await it, schedule all monitors to run in background tasks
                        var task = LaunchMonitorAsync(schedule, cancelToken);
                    }
                }
            }
        }

        /// <summary>
        /// Determine if a schedule is ready to be launched
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private bool IsScheduled(ISchedule schedule)
        {
            var isScheduled = false;
            var diff = DateTime.UtcNow - schedule.LastRuntime;
            foreach (IScheduleTime time in schedule.Times)
            {
                if (diff > time.Interval + schedule.InitDelay)
                {
                    isScheduled = true;
                    schedule.InitDelay = TimeSpan.Zero;
                }
            }

            return isScheduled;
        }

        private HashSet<ISchedule> CreateSchedules(Guid connectionId, List<MonitorConfiguration> monitorConfigurations)
        {
            var builtInInstruments = new[] { CpuUsageModule.ModuleName, MemoryAvailableModule.ModuleName, MemoryInstalledModule.ModuleName, DriveSpaceAvailableModule.ModuleName, DriveSpaceTotalModule.ModuleName };

            var schedules = new HashSet<ISchedule>();
            foreach (var monitor in monitorConfigurations)
            {
                var monitorType = monitor.Configuration
                    .Where(x => x.Key.Equals("Instrument", StringComparison.InvariantCultureIgnoreCase))
                    .Select(x => x.Value)
                    .FirstOrDefault();
                if (string.IsNullOrEmpty(monitorType) || builtInInstruments.Contains(monitorType, StringComparer.InvariantCultureIgnoreCase))
                    continue;
                var schedule = new Schedule(_serviceProvider, monitor.MonitorId, monitor.Monitor, monitorType)
                {
                    InitDelay = TimeSpan.Zero,
                    ConfigurationParameters = new ConfigurationParameters()
                };

                if (monitor.Schedule?.Contains(":") == true)
                    schedule.Times = ScheduleGenerator.GenerateScheduleTimesFromInterval(TimeSpan.Parse(monitor.Schedule));
                else if (!string.IsNullOrEmpty(monitor.Schedule))
                    schedule.Times = ScheduleGenerator.GenerateScheduleTimesFromChrontabFormat(monitor.Schedule);
                else
                    throw new InvalidOperationException("Error: Schedule is not defined. An interval in the format of 00:00:00 must be provided, or in extended chrontab format (sec min hour day month dayofweek). Example: '0/30 0 0 0 0 0' for every 30 seconds..");
                schedule.ConfigurationParameters = new ConfigurationParameters(monitor.Configuration
                    .Select(x => new ConfigurationParameter(x.Key, x.Value)));

                if (!schedules.Contains<ISchedule>(schedule))
                    schedules.Add(schedule);
            }

            return schedules;
        }

        /// <summary>
        /// Remove a cancellation token from the list as the operation is now complete.
        /// </summary>
        /// <param name="scheduleId"></param>
        private void RemoveCancellationToken(Guid scheduleId)
        {
            lock (_cancelTokensLock)
            {
                if (_cancellationTokens.ContainsKey(scheduleId))
                    _cancellationTokens.Remove(scheduleId);
            }
        }

        public override void Dispose()
        {
            Console.WriteLine("Connection disposed!");
            base.Dispose();
            _sendEvent.Dispose();
            _hardwareInformationLock.Dispose();
        }
    }
}
