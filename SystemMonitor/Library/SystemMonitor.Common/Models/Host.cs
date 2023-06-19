using System.Net;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Common.Models
{
    /// <summary>
    /// Represents a host with a hostname/ip pair
    /// </summary>
    public class Host : IHost
    {
        public string Name { get; set; }
        public IPAddress Ip { get; set; } = IPAddress.None;
        public Uri? Hostname { get; set; }

        /// <summary>
        /// Create a Host
        /// </summary>
        /// <param name="name">Display name of host</param>
        /// <param name="host">Network hostname of host</param>
        /// <param name="ip">Network ip of host</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Host(string name, string host, string? ip)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            Name = name;
            if (!string.IsNullOrEmpty(host))
                Hostname = new Uri(host, UriKind.RelativeOrAbsolute);

            if (!string.IsNullOrEmpty(ip))
            {
                if(IPAddress.TryParse(ip, out var ipAddress))
                    Ip = ipAddress;
            }
            if (Equals(Ip, IPAddress.None) && Hostname != null)
                Ip = Util.GetIpFromHostname(Hostname);
        }

        public override string ToString() => $"{Name} ({Ip})";
    }
}
