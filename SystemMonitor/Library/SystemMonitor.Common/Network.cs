using System.Net;

namespace SystemMonitor.Common
{
    /// <summary>
    /// Network utilities
    /// </summary>
    public static class Network
    {
        /// <summary>
        /// Resolve DNS name to IPs, with timeout.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="timeout">Timeout milliseconds</param>
        /// <returns></returns>
        public static IPHostEntry GetIPForHost(string hostname, int timeout = 1000)
        {
            var cachedIP = DnsCache.Instance.GetCachedIP(hostname);
            if (cachedIP != null)
            {
                return cachedIP;
            }
            var instance = new DnsNetworkInstance();

            var ioContext = new DnsNetworkInstance.ResolveState(hostname);
            Dns.BeginGetHostEntry(hostname, new AsyncCallback(instance.GetHostEntryCallback), ioContext);
            var success = instance.GetHostEntryFinished.WaitOne(timeout);

            if (!success || ioContext == null || ioContext.IPs == null)
            {
                // timeout
                return null;
            }
            DnsCache.Instance.AddIP(hostname, ioContext.IPs);
            return ioContext.IPs;
        }


        /// <summary>
        /// Internal instance for async dns resolving
        /// </summary>
        internal class DnsNetworkInstance
        {
            public ManualResetEvent GetHostEntryFinished;
            public DnsNetworkInstance()
            {
                GetHostEntryFinished = new ManualResetEvent(false);
            }

            public void GetHostEntryCallback(IAsyncResult ar)
            {
                var ioContext = (ResolveState)ar.AsyncState;

                try
                {
                    ioContext.IPs = Dns.EndGetHostEntry(ar);
                }
                catch (Exception)
                {

                }
                finally
                {
                    GetHostEntryFinished.Set();
                }
            }

            internal class ResolveState
            {
                string hostName;
                IPHostEntry resolvedIPs;

                public ResolveState(string host)
                {
                    hostName = host;
                    resolvedIPs = new IPHostEntry();
                }

                public IPHostEntry IPs
                {
                    get { return resolvedIPs; }
                    set { resolvedIPs = value; }
                }

                public string Host
                {
                    get { return hostName; }
                    set { hostName = value; }
                }
            }
        }

        /// <summary>
        /// Maintain an internal cache of dns hostname to IP addresses
        /// </summary>
        public class DnsCache
        {
            private const int CacheExpirySeconds = 60 * 15; // 15 minutes
            public static volatile DnsCache _instance;
            private static object _syncRoot = new();

            private object dataLock = new object();
            private Dictionary<HostEntry, IPHostEntry> cachedIPs = new Dictionary<HostEntry, IPHostEntry>();
            public Dictionary<HostEntry, IPHostEntry> CachedIPs
            {
                get { return cachedIPs; }
                set { cachedIPs = value; }
            }
            public static DnsCache Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        lock (_syncRoot)
                        {
                            if (_instance == null)
                                _instance = new DnsCache();
                        }
                    }
                    return _instance;
                }
            }

            private DnsCache()
            {
            }

            public void AddIP(string host, IPHostEntry ip)
            {
                lock (dataLock)
                {
                    if (cachedIPs.Keys.Count(x => x.Hostname == host) == 0)
                        cachedIPs.Add(new HostEntry(host, DateTime.UtcNow.AddSeconds(CacheExpirySeconds)), ip);
                }
            }

            public IPHostEntry GetCachedIP(string host)
            {
                lock (dataLock)
                {
                    if (cachedIPs.Keys.Count(x => x.Hostname == host) > 0)
                    {
                        var key = cachedIPs.Keys.FirstOrDefault(x => x.Hostname == host);
                        if (key != null)
                        {
                            if (DateTime.UtcNow < key.Expiry)
                                return cachedIPs[key];
                            else
                                cachedIPs.Remove(key);
                        }

                    }
                }

                return null;
            }

            public class HostEntry
            {
                public string Hostname { get; set; }
                public DateTime Expiry { get; set; }
                public HostEntry(string hostname, DateTime expiry)
                {
                    Hostname = hostname;
                    Expiry = expiry;
                }
            }
        }

    }
}
