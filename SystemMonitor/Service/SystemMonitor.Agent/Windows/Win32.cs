#if OS_WINDOWS
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace SystemMonitor.Agent.Windows
{
    [StructLayout(LayoutKind.Sequential)]   
    public struct SYSTEM_INFO {
        public  uint  dwOemId;
        public  uint  dwPageSize;
        public  uint  lpMinimumApplicationAddress;
        public	uint  lpMaximumApplicationAddress;
        public  uint  dwActiveProcessorMask;
        public  uint  dwNumberOfProcessors;
        public  uint  dwProcessorType;
        public  uint  dwAllocationGranularity;
        public  uint  dwProcessorLevel;
        public  uint  dwProcessorRevision;		
    }

    public static class Win32
    {
        #region System Information
        [DllImport("kernel32")]
        public static extern void GetSystemInfo(ref SYSTEM_INFO pSI);
        
        public const int PROCESSOR_INTEL_386 = 386;
        public const int PROCESSOR_INTEL_486 = 486;
        public const int PROCESSOR_INTEL_PENTIUM = 586;
        public const int PROCESSOR_MIPS_R4000 = 4000;
        public const int PROCESSOR_ALPHA_21064 = 21064;
        #endregion

        #region Win32 Cpu

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSystemTimes(out ComTypes.FILETIME lpIdleTime,out ComTypes.FILETIME lpKernelTime,out ComTypes.FILETIME lpUserTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetProcessTimes(IntPtr hProcess, out ComTypes.FILETIME lpCreationTime, out ComTypes.FILETIME lpExitTime, out ComTypes.FILETIME lpKernelTime, out ComTypes.FILETIME lpUserTime);
        #endregion

        #region Win32 Memory

        [DllImport("kernel32")]
        public static extern void GlobalMemoryStatus(ref MEMORYSTATUS buf);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]   
        public struct MEMORYSTATUS
        {
            public  uint dwLength;
            public  uint dwMemoryLoad;
            public  uint dwTotalPhys;
            public  uint dwAvailPhys;
            public  uint dwTotalPageFile;
            public  uint dwAvailPageFile;
            public  uint dwTotalVirtual;
            public  uint dwAvailVirtual;
        }

        #endregion
    }
}
#endif
