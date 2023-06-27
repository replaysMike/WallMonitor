#if OS_WINDOWS
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace WallMonitor.Agent.Windows
{
    public class Win32CpuUsage : IDisposable
    {
        private ComTypes.FILETIME _prevSysKernel;
        private ComTypes.FILETIME _prevSysUser;
        private TimeSpan _prevProcTotal;
        private decimal _cpuUsage;
        private DateTime _lastRun;
        private long _runCount;
        private decimal _diff;
        private readonly Process _process;
        private readonly Action<int> _onExitProcess;

        public Win32CpuUsage(Process process, Action<int> onExitProcess)
        {
            _process = process;
            _cpuUsage = -1;
            _lastRun = DateTime.MinValue;
            _prevSysUser.dwHighDateTime = _prevSysUser.dwLowDateTime = 0;
            _prevSysKernel.dwHighDateTime = _prevSysKernel.dwLowDateTime = 0;
            _prevProcTotal = TimeSpan.MinValue;
            _runCount = 0;
            _diff = 0;
            _onExitProcess = onExitProcess;
        }

        public decimal GetUsage()
        {
            try
            {
                var cpuCopy = _cpuUsage;

                if (Interlocked.Increment(ref _runCount) == 1)
                {
                    if (!EnoughTimePassed)
                    {
                        Interlocked.Decrement(ref _runCount);
                        return cpuCopy;
                    }

                    var process = _process;
                    var procTime = TimeSpan.Zero;
                    if (process.HasExited)
                    {
                        _onExitProcess(process.Id);
                        return 0;
                    }

                    procTime = process.TotalProcessorTime;

                    if (!Win32.GetSystemTimes(out _, out var sysKernel, out var sysUser))
                    {
                        Interlocked.Decrement(ref _runCount);
                        return cpuCopy;
                    }

                    if (!IsFirstRun)
                    {
                        var sysKernelDiff = SubtractTimes(sysKernel, _prevSysKernel);
                        var sysUserDiff = SubtractTimes(sysUser, _prevSysUser);
                        var sysTotal = sysKernelDiff + sysUserDiff;
                        var procTotal = procTime.Ticks - _prevProcTotal.Ticks;

                        if (sysTotal > 0)
                        {

                            _diff = procTotal / (decimal)sysTotal;
                            _cpuUsage = _diff;
                        }
                    }

                    _prevProcTotal = procTime;
                    _prevSysKernel = sysKernel;
                    _prevSysUser = sysUser;
                    _lastRun = DateTime.UtcNow;
                    cpuCopy = _cpuUsage;
                }

                Interlocked.Decrement(ref _runCount);
                return cpuCopy;
            }
            catch (Win32Exception ex)
            {
                if (ex.Message.Contains("Access is denied", StringComparison.InvariantCultureIgnoreCase))
                {
                    // no permissions to get cpu usage
                    return -3;
                }

                return -2;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        private static ulong SubtractTimes(ComTypes.FILETIME a, ComTypes.FILETIME b)
        {
            var aInt = ((ulong)a.dwHighDateTime << 32) | (uint)a.dwLowDateTime;
            var bInt = ((ulong)b.dwHighDateTime << 32) | (uint)b.dwLowDateTime;
            return aInt - bInt;
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

        private bool IsFirstRun => (_lastRun == DateTime.MinValue);

        public static DateTime FileTimeToDateTime(ComTypes.FILETIME fileTime)
        {
            //NB! uint conversion must be done on both fields before ulong conversion
            var hFT2 = unchecked((((ulong)(uint)fileTime.dwHighDateTime) << 32) | (uint)fileTime.dwLowDateTime);
            return DateTime.FromFileTimeUtc((long)hFT2);
        }

        public static TimeSpan FileTimeToTimeSpan(ComTypes.FILETIME fileTime)
        {
            //NB! uint conversion must be done on both fields before ulong conversion
            var hFT2 = unchecked((((ulong)(uint)fileTime.dwHighDateTime) << 32) | (uint)fileTime.dwLowDateTime);
            return TimeSpan.FromTicks((long)hFT2);
        }

        public override string ToString() => _cpuUsage.ToString(CultureInfo.InvariantCulture);

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
#endif