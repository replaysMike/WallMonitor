#if OS_WINDOWS
using System.Management;
namespace WallMonitor.Agent.Windows
{
    public class WmiQueries : IDisposable
    {
        public WmiQueries()
        {

        }

        public long GetTotalMemoryInstalled()
        {
            var wmiQuery = "Select * from Win32_ComputerSystem";
            using var searcher = new ManagementObjectSearcher(wmiQuery);
            var retObjectCollection = searcher.Get();
            foreach (var item in retObjectCollection)
            {
                return Convert.ToInt64(item.GetPropertyValue("TotalPhysicalMemory"));
            }

            return 0;
        }

        public string GetMotherboardInfo()
        {
            // https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-baseboard
            // get motherboard information
            var wmiQuery = "Select * from Win32_BaseBoard";
            using var searcher = new ManagementObjectSearcher(wmiQuery);
            var retObjectCollection = searcher.Get();
            foreach (var item in retObjectCollection)
            {
               
            }

            return string.Empty;
        }

        public void Dispose()
        {
        }
    }
}
#endif
