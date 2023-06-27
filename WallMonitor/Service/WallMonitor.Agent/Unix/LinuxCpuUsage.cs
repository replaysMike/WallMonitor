using System.Diagnostics;
using Universe.CpuUsage;

namespace WallMonitor.Agent.Unix
{
    public class LinuxCpuUsage : IDisposable
    {
        public const int RESOURCE_USAGE_FIELDS_COUNT = 18;
        private long _runCount;
        private TimeSpan _startProcTime;
        private TimeSpan _prevProcTotal;
        private DateTime _lastRun;
        private decimal _cpuUsage;
        private readonly Process _process;
        private readonly Action<int> _onExitProcess;

        public LinuxCpuUsage(Process process, Action<int> onExitProcess)
        {
            _process = process;
            _onExitProcess = onExitProcess;
        }

        public decimal GetUsage()
        {
            var cpuCopy = _cpuUsage;
            if (_runCount == 0)
            {
                _startProcTime = _process.TotalProcessorTime;
                _lastRun = DateTime.UtcNow;
            }

            Interlocked.Increment(ref _runCount);

            if (!EnoughTimePassed)
            {
                //Interlocked.Decrement(ref _runCount);
                return cpuCopy;
            }

            var process = _process;
            if (process.HasExited)
            {
                _onExitProcess(process.Id);
                return 0;
            }

            var procTime = process.TotalProcessorTime;

            var cpuUsedMs = (procTime - _startProcTime).TotalMilliseconds;
            var totalMsPassed = (DateTime.UtcNow - _lastRun).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            _cpuUsage = (decimal)cpuUsageTotal;
            // Console.WriteLine($"Made it here {cpuUsageTotal}");
            
            //_prevProcTotal = procTime;
            //_prevSysKernel = sysKernel;
            //_prevSysUser = sysUser;
            //_lastRun = DateTime.UtcNow;
            //_startCpuUsage = process.TotalProcessorTime;
            cpuCopy = _cpuUsage;
            _runCount = 0;

            return cpuCopy;
        }

        private bool EnoughTimePassed
        {
            get
            {
                const int minimumElapsedMs = 250;
                var sinceLast = DateTime.UtcNow - _lastRun;
                return sinceLast.TotalMilliseconds > minimumElapsedMs;
            }
        }

        public unsafe CpuUsage? GetCpuUsageInternal()
        {
            var scope = 1;
            if (IntPtr.Size == 4)
            {
                int* rawResourceUsage = stackalloc int[RESOURCE_USAGE_FIELDS_COUNT];
                var result = LinuxResourceUsageInterop.getrusage_heapless(scope, new IntPtr(rawResourceUsage));
                if (result != 0) return null;
                return new CpuUsage()
                {
                    UserUsage = new TimeValue() {Seconds = *rawResourceUsage, MicroSeconds = rawResourceUsage[1]},
                    KernelUsage = new TimeValue() {Seconds = rawResourceUsage[2], MicroSeconds = rawResourceUsage[3]},
                };
            }
            else
            {
                long* rawResourceUsage = stackalloc long[RESOURCE_USAGE_FIELDS_COUNT];
                var result = LinuxResourceUsageInterop.getrusage_heapless(scope, new IntPtr(rawResourceUsage));
                if (result != 0) return null;
                // microseconds are 4 bytes length on mac os and 8 bytes on linux
                return new CpuUsage()
                {
                    UserUsage = new TimeValue() {Seconds = *rawResourceUsage, MicroSeconds = rawResourceUsage[1] & 0xFFFFFFFF},
                    KernelUsage = new TimeValue() {Seconds = rawResourceUsage[2], MicroSeconds = rawResourceUsage[3] & 0xFFFFFFFF},
                };
            }
        }


        public void Dispose()
        {
        }
    }
}
