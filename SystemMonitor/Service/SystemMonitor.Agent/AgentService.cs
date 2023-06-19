using System.Data;
using System.Reflection;
using SystemMonitor.Common.IO;
using SystemMonitor.Common.IO.AgentMessages;
using SystemMonitor.Common.IO.Server.Transport;

namespace SystemMonitor.Agent
{
    public class AgentService : BackgroundService
    {
        private readonly ILogger<AgentService> _logger;
        private readonly Configuration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IList<IModule> _modules = new List<IModule>();
        private readonly Thread _serverThread;
        private int _connectionCount;
        private AgentTcpServer? _tcpServer;

        public AgentService(ILogger<AgentService> logger, Configuration configuration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Thread.CurrentThread.Name = "SystemMonitor.Agent";
            _serverThread = new Thread(ServerThread);
            _logger.LogInformation($"Agent service {Assembly.GetExecutingAssembly().GetName().Version?.ToString()} created.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Starting agent service...");
                _modules.Add(await CpuUsageModule.CreateAsync());
                _modules.Add(await MemoryInstalledModule.CreateAsync());
                _modules.Add(await MemoryAvailableModule.CreateAsync());
                _modules.Add(await DriveSpaceTotalModule.CreateAsync());
                _modules.Add(await DriveSpaceAvailableModule.CreateAsync());
                _serverThread.Start(stoppingToken);
                _logger.LogInformation("Agent service started!");

                var noClientsConnectedDisplayed = false;
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_connectionCount <= 0 && !_configuration.AlwaysMonitor)
                    {
                        if (!noClientsConnectedDisplayed)
                            _logger.LogInformation($"No clients connected, pausing monitoring...");
                        noClientsConnectedDisplayed = true;

                        await Task.Delay(250, stoppingToken);
                        continue;
                    }

                    noClientsConnectedDisplayed = false;
                    _logger.LogInformation($"{_connectionCount} clients connected.");

                    Update();

                    await Task.Delay(1000, stoppingToken);
                }

                _logger.LogInformation("Stopping agent service...");
                foreach (var module in _modules)
                {
                    module.Dispose();
                }

                _modules.Clear();
                _logger.LogInformation("Agent service stopped.");
            }
            catch (TaskCanceledException)
            {
                // this is normal and ok
                _logger.LogInformation("Agent service stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal service exception!");
                // returning a non-zero exit code will stop the host, and allow recovery options
                Environment.Exit(1);
            }
        }

        private void Update()
        {
            var cpuModule = _modules.FirstOrDefault(x => x.Name == "Cpu");
            if (cpuModule != null)
            {
                var cpuUsage = cpuModule.CurrentValue;
                if (cpuModule.ErrorCode < 0)
                    Console.WriteLine($"WARN: CPU Usage returned error: {cpuModule.ErrorCode}");
                Console.WriteLine($"CPU Usage: {cpuUsage}");
            }

            var totalMemoryInstalledModule = _modules.FirstOrDefault(x => x.Name == "MemoryInstalled");
            if (totalMemoryInstalledModule != null)
            {
                var totalMemoryInstalled = totalMemoryInstalledModule.CurrentValue;
                Console.WriteLine($"Total Memory Installed: {totalMemoryInstalled}");
            }

            var totalMemoryAvailableModule = _modules.FirstOrDefault(x => x.Name == "MemoryAvailable");
            if (totalMemoryAvailableModule != null)
            {
                var totalMemoryAvailable = totalMemoryAvailableModule.CurrentValue;
                Console.WriteLine($"Total Memory Available: {totalMemoryAvailable}");
            }

            var totalDiskInstalledModule = _modules.FirstOrDefault(x => x.Name == "DiskInstalled");
            if (totalDiskInstalledModule != null)
            {
                var totalDisksInstalled = totalDiskInstalledModule.CurrentDictionary;
                Console.WriteLine($"Total Disk Installed: ");

                foreach (var disk in totalDisksInstalled)
                    Console.WriteLine($"  - {disk.Key}: {IOHelper.GetFriendlyBytes(disk.Value)}");
            }

            var totalDiskAvailableModule = _modules.FirstOrDefault(x => x.Name == "DiskAvailable");
            if (totalDiskAvailableModule != null)
            {
                var totalDisksAvailable = totalDiskAvailableModule.CurrentDictionary;
                Console.WriteLine("Total Disk Available: ");
                foreach (var disk in totalDisksAvailable)
                    Console.WriteLine($"  - {disk.Key}: {IOHelper.GetFriendlyBytes(disk.Value)}");
            }

            var info = new HardwareInformationMessage
            {
                Cpu = cpuModule.Value,
                TotalMemoryInstalled = (ulong)totalMemoryInstalledModule.Value,
                TotalMemoryAvailable = (ulong)totalMemoryAvailableModule.Value,
                NumberOfDrives = (byte)totalDiskInstalledModule.CurrentDictionary.Count,
                Drives = totalDiskInstalledModule.CurrentDictionary.Select((value, index) => new { value.Key, index })
                    .ToDictionary(pair => (byte)pair.index, pair => pair.Key),
                DriveSpaceTotal = totalDiskInstalledModule.CurrentDictionary.Select((value, index) => new { value.Value, index })
                    .ToDictionary(pair => (byte)pair.index, pair => (ulong)pair.Value),
                DriveSpaceAvailable = totalDiskAvailableModule.CurrentDictionary.Select((value, index) => new { value.Value, index })
                    .ToDictionary(pair => (byte)pair.index, pair => (ulong)pair.Value)
            };

            _tcpServer?.SetHardwareInformation(info);
        }

        private async void ServerThread(object? state)
        {
            _logger.LogInformation("Server thread started, waiting for connections...");
            var cancellationToken = (CancellationToken?)state;
            if (cancellationToken == null) return;

            // set up the tcp server
            try
            {
                _tcpServer = new AgentTcpServer(_configuration, _serviceProvider);

                _tcpServer.OnConnect += TcpServer_OnConnect;
                _tcpServer.OnDisconnect += TcpServer_OnDisconnect;
                await _tcpServer.OpenAsync(cancellationToken.Value);
            }
            finally
            {
                _tcpServer?.Dispose();
            }
            _logger.LogInformation("Server thread closed.");
        }

        private void TcpServer_OnDisconnect(object? sender, ConnectionEventArgs e)
        {
            _logger.LogInformation($"Client from '{e.RemoteEndPoint}' disconnected.");
            Interlocked.Decrement(ref _connectionCount);
        }

        private void TcpServer_OnConnect(object? sender, ConnectionEventArgs e)
        {
            _logger.LogInformation($"Client connected from '{e.RemoteEndPoint}'.");
            Interlocked.Increment(ref _connectionCount);
        }
    }
}