#if OS_WINDOWS
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security;

namespace WallMonitor.Agent.Windows
{
    /// 
    /// A safe wrapper around a PDH Log handle.
    /// 
    /// 
    /// Use this along with PdhBindInputDataSource and the "H" APIs to bind multiple logs together
    /// 
    public class PdhLogHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public PdhLogHandle() : base(true)
        {

        }

        protected override bool ReleaseHandle()
        {
            return PdhApi.PdhCloseLog(handle, PdhApi.PDH_FLAGS_CLOSE_QUERY) == 0;
        }
    }

    /// 
    /// A safe wrapper arounda query handle
    /// 
    public class PdhQueryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public PdhQueryHandle()
            : base(true)
        {

        }

        protected override bool ReleaseHandle()
        {
            return PdhApi.PdhCloseQuery(handle) == 0;
        }
    }

    /// 
    /// A safe handle around a counter
    /// 
    public class PdhCounterHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public PdhCounterHandle()
            : base(true)
        {

        }

        protected override bool ReleaseHandle()
        {
            return PdhApi.PdhRemoveCounter(handle) == 0;
        }
    }

    /// 
    /// The value of a counter as returned by  API.
    /// 
    /// In C/C++ this is a union.
    [StructLayout(LayoutKind.Explicit)]
    public struct PDH_FMT_COUNTERVALUE
    {
        [FieldOffset(0)]
        public UInt32 CStatus;
        [FieldOffset(1)]
        public int longValue;
        [FieldOffset(1)]
        public double doubleValue;
        [FieldOffset(1)]
        public long longLongValue;
        [FieldOffset(1)]
        public IntPtr AnsiStringValue;
        [FieldOffset(1)]
        public IntPtr WideStringValue;
    }

    /// 
    /// The requested format for the  API.
    /// 
    [Flags()]
    public enum PdhFormat : uint
    {
        PDH_FMT_RAW = 0x00000010,
        PDH_FMT_ANSI = 0x00000020,
        PDH_FMT_UNICODE = 0x00000040,
        PDH_FMT_LONG = 0x00000100,
        PDH_FMT_DOUBLE = 0x00000200,
        PDH_FMT_LARGE = 0x00000400,
        PDH_FMT_NOSCALE = 0x00001000,
        PDH_FMT_1000 = 0x00002000,
        PDH_FMT_NODATA = 0x00004000
    }

    /// 
    /// Static class containing some usefull PDH API's
    /// 
    [SuppressUnmanagedCodeSecurity()]
    internal class PdhApi
    {
        #region A few common flags and status codes
        public const UInt32 PDH_FLAGS_CLOSE_QUERY = 1;
        public const UInt32 PDH_NO_MORE_DATA = 0xC0000BCC;
        public const UInt32 PDH_INVALID_DATA = 0xC0000BC6;
        public const UInt32 PDH_ENTRY_NOT_IN_LOG_FILE = 0xC0000BCD;
        #endregion

        /// 
        /// Opens a query handle
        /// 
        /// 
        /// 
        /// 
        /// 
        [DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern UInt32 PdhOpenQuery(string szDataSource, IntPtr dwUserData, out PdhQueryHandle phQuery);

        /// 
        /// Opens a query against a bound input source.
        /// 
        /// 
        /// 
        /// 
        /// 
        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhOpenQueryH(PdhLogHandle hDataSource, IntPtr dwUserData, out PdhQueryHandle phQuery);

        /// 
        /// Binds multiple logs files together.
        /// 
        /// Use this along with the API's ending in 'H' to string multiple files together.
        /// 
        /// 
        /// 
        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhBindInputDataSource(out PdhLogHandle phDataSource, string szLogFileNameList);

        /// 
        /// Closes a handle to a log
        /// 
        /// 
        /// 
        /// 
        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhCloseLog(IntPtr hLog, long dwFlags);

        /// 
        /// Closes a handle to the log
        /// 
        /// 
        /// 
        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhCloseQuery(IntPtr hQuery);

        /// 
        /// Removes a counter from the given query.
        /// 
        /// 
        /// 
        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhRemoveCounter(IntPtr hQuery);

        /// 
        /// Adds a counter the query and passes out a handle to the counter.
        /// 
        /// 
        /// 
        /// 
        /// 
        /// 
        [DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern UInt32 PdhAddCounter(PdhQueryHandle hQuery, string szFullCounterPath, IntPtr dwUserData, out PdhCounterHandle phCounter);

        /// 
        /// Retrieves a sample from the source.
        /// 
        /// 
        /// 
        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhCollectQueryData(PdhQueryHandle phQuery);

        /// 
        /// Retrieves a specific counter value in the specified format.
        /// 
        /// 
        /// 
        /// 
        /// 
        /// 
        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhGetFormattedCounterValue(PdhCounterHandle phCounter, PdhFormat dwFormat, IntPtr lpdwType, out PDH_FMT_COUNTERVALUE pValue);
    }
}
#endif