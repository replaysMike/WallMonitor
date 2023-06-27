using WallMonitor.Common.IO;
using WallMonitor.Common.IO.AgentMessages;

namespace WallMonitor.Common.Sdk
{
    public interface IAgentMonitorAsync
    {
        IAgentsProvider AgentsProvider { get; set; }
    }

    public class AgentsProvider : IAgentsProvider
    {
        private readonly List<TcpAgentClient> _tcpAgentClients = new();

        public AgentsProvider()
        {
        }

        public AgentsProvider(List<TcpAgentClient> tcpAgentClients)
        {
            _tcpAgentClients = tcpAgentClients;
        }

        public void SetRemoteAgents(List<TcpAgentClient> tcpAgentClients)
        {
            _tcpAgentClients.Clear();
            _tcpAgentClients.AddRange(tcpAgentClients);
        }

        public bool IsConnected(IHost host)
        {
            if (host.Hostname != null)
            {
                // get the agent by host name
                var client = _tcpAgentClients.FirstOrDefault(x => x.HostName?.Equals(host.Hostname.OriginalString, StringComparison.InvariantCultureIgnoreCase) == true);
                if (client != null)
                    return client.IsConnected;
            }

            if (host.Ip != null)
            {
                // get the agent by host name
                var client = _tcpAgentClients.FirstOrDefault(x => x.IpAddress?.Equals(host.Ip.ToString()) == true);
                if (client != null)
                    return client.IsConnected;
            }

            return false;
        }

        public HardwareInformationMessage? GetHardwareInformation(IHost host)
        {
            if (host.Hostname != null)
            {
                // get the agent by host name
                var client = _tcpAgentClients.FirstOrDefault(x => x.HostName?.Equals(host.Hostname.OriginalString, StringComparison.InvariantCultureIgnoreCase) == true);
                if (client != null)
                {
                    return client.TryGetHardwareInformation();
                }
            }

            if (host.Ip != null)
            {
                // get the agent by host name
                var client = _tcpAgentClients.FirstOrDefault(x => x.IpAddress?.Equals(host.Ip.ToString()) == true);
                if (client != null)
                    return client.TryGetHardwareInformation();
            }

            // no matching agent found
            return null;
        }

        public async Task<HardwareInformationMessage?> GetHardwareInformationAsync(IHost host)
        {
            if (host.Hostname != null)
            {
                // get the agent by host name
                var client = _tcpAgentClients.FirstOrDefault(x => x.HostName?.Equals(host.Hostname.OriginalString, StringComparison.InvariantCultureIgnoreCase) == true);
                if (client != null)
                {
                    return await client.TryGetHardwareInformationAsync();
                }
            }

            if (host.Ip != null)
            {
                // get the agent by host name
                var client = _tcpAgentClients.FirstOrDefault(x => x.IpAddress?.Equals(host.Ip.ToString()) == true);
                if (client != null)
                    return await client.TryGetHardwareInformationAsync();
            }

            // no matching agent found
            return null;
        }
    }

    public interface IAgentsProvider
    {
        /// <summary>
        /// Get hardware information for a host
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        HardwareInformationMessage? GetHardwareInformation(IHost host);

        /// <summary>
        /// Get hardware information for a host
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        Task<HardwareInformationMessage?> GetHardwareInformationAsync(IHost host);

        /// <summary>
        /// Returns true if connected to host
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        bool IsConnected(IHost host);
    }
}
