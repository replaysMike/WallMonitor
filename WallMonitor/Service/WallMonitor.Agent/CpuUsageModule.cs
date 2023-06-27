using System.Diagnostics;
#if OS_LINUX
using WallMonitor.Agent.Unix;
#endif
#if OS_WINDOWS
using WallMonitor.Agent.Windows;
#endif

namespace WallMonitor.Agent
{

    public class CpuUsageModule : IDisposable, IModule
    {
        public const string ModuleName = "Cpu";
        public string Name => ModuleName;
        public string CurrentValue => $"{Value * 100:n2}%";
        public IDictionary<string, long> CurrentDictionary => throw new NotImplementedException();

        public double Value { get; private set; }
        public int ErrorCode { get; private set; }

        private readonly object _lock = new object();
        private readonly System.Timers.Timer _timer = new(TimeSpan.FromSeconds(1));

#if OS_LINUX
        private readonly Dictionary<int, LinuxCpuUsage> _linuxCpuUsages = new ();
#endif
#if OS_WINDOWS
        private readonly PerformanceCounterHost _performanceCounterHost;
        private readonly Dictionary<int, Win32CpuUsage> _win32CpuUsages = new();
#endif

        private CpuUsageModule()
        {
#if OS_WINDOWS
            var processes = Process.GetProcesses();
            foreach (var process in processes.Where(x => x.Id != 0))
                _win32CpuUsages.Add(process.Id, new Win32CpuUsage(process, RemoveProcess));
            _performanceCounterHost = new PerformanceCounterHost("CPU Processor Time %");
#endif
        }

        public static async Task<IModule> CreateAsync()
        {
            var me = new CpuUsageModule();
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
#if OS_WINDOWS
            AddNewProcesses();
#endif
#if OS_LINUX
            AddNewProcesses();
#endif
            await UpdateAsync();
            _timer.Start();
        }

#if OS_LINUX
        private void AddNewProcesses()
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes.Where(x => x.Id != 0))
            {
                if (!_linuxCpuUsages.ContainsKey(process.Id))
                {
                    _linuxCpuUsages.Add(process.Id, new LinuxCpuUsage(process, RemoveProcess));
                }
            }
        }

        private void RemoveProcess(int processId)
        {
            if (_linuxCpuUsages.ContainsKey(processId))
            {
                _linuxCpuUsages[processId].Dispose();
                _linuxCpuUsages.Remove(processId);
            }
        }
#endif

#if OS_WINDOWS
        private void AddNewProcesses()
        {
            Monitor.Enter(_lock);
            try
            {
                var processes = Process.GetProcesses();
                foreach (var process in processes.Where(x => x.Id != 0))
                {
                    if (!_win32CpuUsages.ContainsKey(process.Id))
                    {
                        _win32CpuUsages.Add(process.Id, new Win32CpuUsage(process, RemoveProcess));
                    }
                }
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }

        private void RemoveProcess(int processId)
        {
            Monitor.Enter(_lock);
            try
            {
                if (_win32CpuUsages.ContainsKey(processId))
                {
                    _win32CpuUsages[processId].Dispose();
                    _win32CpuUsages.Remove(processId);
                }
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }
#endif

        private async Task UpdateAsync()
        {
#if OS_WINDOWS
            Monitor.Enter(_lock);
            try
            {
                Value = await UpdateCpuUsageWindowsAsync();
                _performanceCounterHost.ReportData(Value * 100d);
            }
            finally
            {
                Monitor.Exit(_lock);
            }
#endif
#if OS_LINUX
            Value = await UpdateCpuUsageUnixAsync();
#endif
        }

#if OS_WINDOWS
        private async Task<double> UpdateCpuUsageWindowsAsync()
        {
            ErrorCode = 0;
            var usages = new List<decimal>();
            foreach (var process in _win32CpuUsages)
            {
                var val = process.Value.GetUsage();
                if (val < 0)
                {
                    ErrorCode = (int)val;
                }
                else
                {
                    usages.Add(val);
                }
            }

            // filter out negative values
            var total = usages.Where(x => x > 0).Sum();
            return (double)Math.Min(total, 100);
        }
#endif

#if OS_LINUX
        private async Task<double> UpdateCpuUsageUnixAsync()
        {
            var usages = new List<decimal>();
            foreach (var process in _linuxCpuUsages)
            {
                var val = process.Value.GetUsage();
                usages.Add(val);
            }

            // filter out negative values
            var total = usages.Where(x => x > 0).Sum();
            return (double)Math.Min(total, 100);
        }
#endif

        public void Dispose()
        {
            _timer.Dispose();
#if OS_WINDOWS
            _performanceCounterHost.Dispose();
#endif
        }
    }
}
