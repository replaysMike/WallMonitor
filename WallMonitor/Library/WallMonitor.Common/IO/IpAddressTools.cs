using NetTools;
using System.Collections.Concurrent;
using System.Net;

namespace WallMonitor.Common.IO
{
    public static class IpAddressTools
    {
        private static readonly ConcurrentDictionary<string, IPAddressRange> AllowedCache = new ();

        /// <summary>
        /// Returns true if ip address is allowed to connect
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="allowed">List of ip address/subnet/ip ranges to allow connections from</param>
        /// <param name="alwaysAllowLocalhost">True to always allow localhost addresses</param>
        /// <returns></returns>
        public static bool IsIpAddressAllowed(string? ipAddress, List<string> allowed, bool alwaysAllowLocalhost = false)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return false;
            var ipAddressObj = IPAddress.Parse(ipAddress);
            return IsIpAddressAllowed(ipAddressObj, allowed, alwaysAllowLocalhost);
        }

        /// <summary>
        /// Returns true if ip address is allowed to connect
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="allowed">List of ip address/subnet/ip ranges to allow connections from</param>
        /// <param name="alwaysAllowLocalhost">True to always allow localhost addresses</param>
        /// <returns></returns>
        public static bool IsIpAddressAllowed(IPAddress ipAddress, List<string> allowed, bool alwaysAllowLocalhost = false)
        {
            if (alwaysAllowLocalhost && IPAddress.IsLoopback(ipAddress))
                return true;
            foreach (var ipAllowStr in allowed)
            {
                // * or "" allow from any
                if (string.IsNullOrEmpty(ipAllowStr) || ipAllowStr == "*") return true;
                
                IPAddressRange range;
                if (AllowedCache.ContainsKey(ipAllowStr))
                {
                    range = AllowedCache[ipAllowStr];
                }
                else
                {
                    range = IPAddressRange.Parse(ipAllowStr);
                    AllowedCache.TryAdd(ipAllowStr, range);
                }
                if (range.Contains(ipAddress))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
