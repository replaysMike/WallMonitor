using System.Collections;
using System.Runtime.InteropServices;

namespace SystemMonitor.Agent.Unix
{
    public class LinuxResourceUsageInterop
    {
        // For integration tests only
        public static IList GetRawUsageResources(int scope = RUSAGE_THREAD)
        {
            if (IntPtr.Size == 4)
            {
                var ret = new RawLinuxResourceUsage_32();
                ret.Raw = new int[18];
                var result = getrusage32(scope, ref ret);
                if (result != 0) return null;
                Console.WriteLine($"getrusage returns {result}");
                return ret.Raw;
            }
            else
            {
                var ret = new RawLinuxResourceUsage_64();
                ret.Raw = new long[18];
                var result = getrusage64(scope, ref ret);
                if (result != 0) return null;
                Console.WriteLine($"getrusage returns {result}");
                return ret.Raw;
            }
        }

        public const int RUSAGE_SELF = 0;
        public const int RUSAGE_CHILDREN = -1;
        public const int RUSAGE_BOTH = -2; /* sys_wait4() uses this */
        public const int RUSAGE_THREAD = 1; /* only the calling thread */


        [DllImport("libc", SetLastError = true, EntryPoint = "getrusage")]
        public static extern int getrusage32(int who, ref RawLinuxResourceUsage_32 resourceUsage);

        [DllImport("libc", SetLastError = true, EntryPoint = "getrusage")]
        public static extern int getrusage32_heapless(int who, IntPtr resourceUsage);

        [DllImport("libc", SetLastError = true, EntryPoint = "getrusage")]
        public static extern int getrusage64(int who, ref RawLinuxResourceUsage_64 resourceUsage);

        [DllImport("libc", SetLastError = true, EntryPoint = "getrusage")]

        public static extern int getrusage64_heapless(int who, IntPtr resourceUsage);

        [DllImport("libc", SetLastError = true, EntryPoint = "getrusage")]
        public static extern int getrusage_heapless(int who, IntPtr resourceUsage);
    }

    // https://github.com/mono/mono/issues?utf8=%E2%9C%93&q=getrusage
    // https://elixir.bootlin.com/linux/v2.6.24/source/include/linux/time.h#L19
    // https://stackoverflow.com/questions/1468443/per-thread-cpu-statistics-in-linux
    // ! http://man7.org/linux/man-pages/man2/getrusage.2.html
    // 

    [StructLayout(LayoutKind.Sequential)]
    public struct RawLinuxResourceUsage_64
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I8, SizeConst = 18)]
        public long[] Raw;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawLinuxResourceUsage_32
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = 18)]
        public int[] Raw;
    }
}
