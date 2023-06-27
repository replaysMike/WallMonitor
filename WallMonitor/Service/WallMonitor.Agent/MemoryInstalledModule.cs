using WallMonitor.Common.IO;
#if OS_LINUX
using System.Diagnostics;
#endif

#if OS_WINDOWS
using WallMonitor.Agent.Windows;
using static WallMonitor.Agent.Windows.Win32;
#endif

namespace WallMonitor.Agent
{

    public class MemoryInstalledModule : IDisposable, IModule
    {
        public const string ModuleName = "MemoryInstalled";
        public string Name => ModuleName;
        public string CurrentValue => IOHelper.GetFriendlyBytes((long)Value);
        public IDictionary<string, long> CurrentDictionary => throw new NotImplementedException();

        public double Value { get; private set; }
        public int ErrorCode { get; private set; }

        // use a long timer, memory size will only ever change on capable hot addable hardware (such as VMs)
        private readonly System.Timers.Timer _timer = new(TimeSpan.FromMinutes(1));

        private MemoryInstalledModule() { }

        public static async Task<IModule> CreateAsync()
        {
            var me = new MemoryInstalledModule();
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
            Value = await UpdateMemoryUsageWindowsAsync();
#endif
#if OS_LINUX
            Value = await UpdateMemoryUsageUnixAsync();
#endif
        }

#if OS_WINDOWS
        private async Task<double> UpdateMemoryUsageWindowsAsync()
        {
            var mem = new MEMORYSTATUSEX();
            if (Win32.GlobalMemoryStatusEx(mem))
            {
                return mem.ullTotalPhys;
            }
            return 0;
        }
#endif

#if OS_LINUX
        private async Task<double> UpdateMemoryUsageUnixAsync()
        {
            var output = "";
 
            var info = new ProcessStartInfo("free -m");
            info.FileName = "/bin/bash";
            info.Arguments = "-c \"free -m\"";
            info.RedirectStandardOutput = true;
        
            using(var process = Process.Start(info))
            {                
                output = process.StandardOutput.ReadToEnd();
                //Console.WriteLine(output);
            }
 
            var lines = output.Split("\n");
            var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);
    
            var metrics = new MemoryMetrics();
            metrics.Total = double.Parse(memory[1]) * 1024 * 1024;
            metrics.Used = double.Parse(memory[2]) * 1024 * 1024;
            metrics.Free = double.Parse(memory[3]) * 1024 * 1024;
            return metrics.Total;
        }
#endif

        public void Dispose()
        {
            _timer.Dispose();
        }

        private class MemoryMetrics
        {
            public double Total;
            public double Used;
            public double Free;
        }
    }
}
