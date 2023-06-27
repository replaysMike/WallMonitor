using SystemMonitor.Common.IO;
#if OS_LINUX
using System.Diagnostics;
#endif

#if OS_WINDOWS
using SystemMonitor.Agent.Windows;
using static SystemMonitor.Agent.Windows.Win32;
#endif

namespace SystemMonitor.Agent
{

    public class MemoryAvailableModule : IDisposable, IModule
    {
        public const string ModuleName = "MemoryAvailable";
        public string Name => ModuleName;
        public string CurrentValue => IOHelper.GetFriendlyBytes((long)Value);
        public IDictionary<string, long> CurrentDictionary => throw new NotImplementedException();

        public double Value { get; private set; }
        public int ErrorCode { get; private set; }

        private readonly System.Timers.Timer _timer = new(TimeSpan.FromSeconds(1));
#if OS_WINDOWS
        private readonly PerformanceCounterHost _performanceCounterHost;
#endif
        private MemoryAvailableModule()
        {
#if OS_WINDOWS
            _performanceCounterHost = new PerformanceCounterHost("Memory Available");
#endif
        }

        public static async Task<IModule> CreateAsync()
        {
            var me = new MemoryAvailableModule();
            await me.InitializeAsync();
            return me;
        }

        private async Task InitializeAsync()
        {
            // run immediately
            await UpdateAsync();

            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private async void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            await UpdateAsync();
            _timer.Start();
        }

        private async Task UpdateAsync()
        {
#if OS_WINDOWS
            Value = await UpdateMemoryAvailableWindowsAsync();
            _performanceCounterHost.ReportData((long)Value);
#endif
#if OS_LINUX
            Value = await UpdateMemoryAvailableUnixAsync();
#endif
        }

#if OS_WINDOWS
        private async Task<double> UpdateMemoryAvailableWindowsAsync()
        {
            var mem = new MEMORYSTATUSEX();
            if (Win32.GlobalMemoryStatusEx(mem))
            {
                return mem.ullAvailPhys;
            }
            return 0;
        }
#endif

#if OS_LINUX
        private async Task<double> UpdateMemoryAvailableUnixAsync()
        {
            var output = "";
 
            var info = new ProcessStartInfo("free -m")
            {
                FileName = "/bin/bash",
                Arguments = "-c \"free -m\"",
                RedirectStandardOutput = true
            };

            using(var process = Process.Start(info))
            {                
                output = await process?.StandardOutput.ReadToEndAsync()!;
                // Console.WriteLine(output);
            }
 
            var lines = output.Split("\n");
            var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);
    
            var metrics = new MemoryMetrics();
            metrics.Total = double.Parse(memory[1]) * 1024 * 1024;
            metrics.Used = double.Parse(memory[2]) * 1024 * 1024;
            metrics.Free = double.Parse(memory[3]) * 1024 * 1024;
            return metrics.Free;
        }
#endif

        public void Dispose()
        {
            _timer.Dispose();
#if OS_WINDOWS
            _performanceCounterHost.Dispose();
#endif
        }

        private class MemoryMetrics
        {
            public double Total;
            public double Used;
            public double Free;
        }
    }
}
