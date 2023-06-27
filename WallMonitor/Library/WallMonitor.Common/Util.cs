using System.Net;
using System.Net.Sockets;
using System.Text;
using WallMonitor.Common.Sdk;

namespace WallMonitor.Common
{
    public static class Util
    {
        /// <summary>
        /// Get friendly elapsed time
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static string GetFriendlyElapsedTime(TimeSpan timeSpan)
        {
            var builder = new StringBuilder();
            if (timeSpan.Days > 0)
            {
                if (timeSpan.Hours > 1)
                    builder.Append($"{timeSpan.Days:n0} days");
                else
                    builder.Append($"{timeSpan.Days:n0} day");
            }
            if (timeSpan.Hours > 0)
            {
                if (builder.Length > 0) builder.Append(", ");
                if (timeSpan.Hours > 1)
                    builder.Append($"{timeSpan.Hours} hours");
                else
                    builder.Append($"{timeSpan.Hours} hour");
            }
            if (timeSpan.Minutes > 0 && timeSpan.TotalHours < 24)
            {
                if (builder.Length > 0) builder.Append(", ");
                if (timeSpan.Minutes > 1)
                    builder.Append($"{timeSpan.Minutes} minutes");
                else
                    builder.Append($"{timeSpan.Minutes} minute");
            }
            if (timeSpan.Seconds > 0 && timeSpan.TotalMinutes < 10)
            {
                if (builder.Length > 0) builder.Append(", ");
                if (timeSpan.Seconds > 1)
                    builder.Append($"{timeSpan.Seconds} seconds");
                else
                    builder.Append($"{timeSpan.Seconds} second");
            }
            if (timeSpan.Milliseconds > 0 && timeSpan.TotalSeconds < 1)
            {
                if (builder.Length > 0) builder.Append(", ");
                builder.Append($"{timeSpan.Milliseconds} ms");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Get IP Address for hostname
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IPAddress GetIpFromHostname(Uri url)
        {
            var hostNameType = Uri.CheckHostName(url.OriginalString);
            if (hostNameType == UriHostNameType.Dns || hostNameType == UriHostNameType.Unknown)
            {
                var address = IPAddress.None;
                if (url.IsAbsoluteUri)
                {
                    IPAddress.TryParse(url.DnsSafeHost, out address);
                }

                if (address == null || address.Equals(IPAddress.None))
                {
                    var hostEntry = Network.GetIPForHost(url.IsAbsoluteUri ? url.DnsSafeHost : url.OriginalString, 1000);
                    if (hostEntry?.AddressList?.Length > 0)
                    {
                        address = hostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                    }
                }

                return address ?? IPAddress.None;
            }
            else if (hostNameType == UriHostNameType.IPv4 || hostNameType == UriHostNameType.IPv6)
            {
                return IPAddress.Parse(url.Host);
            }

            throw new NotSupportedException($"Unsupported hostname type '{hostNameType}'");
        }

        /// <summary>
        /// Get IP Address for host object
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static IPAddress HostToIp(IHost host)
        {
            var address = IPAddress.None;
            if (host.Ip != null)
            {
                address = host.Ip;
            }
            else if (host.Hostname != null)
            {
                address = GetIpFromHostname(host.Hostname);
            }

            return address;
        }

        /// <summary>
        /// Get name of TCP port
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static string GetWellKnownPortName(int port)
        {

            if (_wellKnownPorts.ContainsKey(port))
                return _wellKnownPorts[port];
            else
                return port.ToString();
        }

        /// <summary>
        /// Get TCP port from name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetPortFromWellKnown(string name)
        {

            if (_wellKnownPorts.ContainsValue(name))
                return _wellKnownPorts.FirstOrDefault(x => string.Equals(x.Value, name, StringComparison.CurrentCultureIgnoreCase)).Key;
            else
                return -1;
        }

        #region Well Known ports

        private static readonly Dictionary<int, string> _wellKnownPorts = new()
            {
                {7,"echo"},
                {9,"discard"},
                {11,"systat"},
                {13,"daytime"},
                {17,"qotd"},
                {19,"chargen"},
                {20,"ftp-data"},
                {21,"ftp"},
                {22,"ssh"},
                {23,"telnet"},
                {25,"smtp"},
                {37,"time"},
                {39,"rlp"},
                {42,"nameserver"},
                {43,"nicname"},
                {53,"domain"},
                {67,"bootps"},
                {68,"bootpc"},
                {69,"tftp"},
                {70,"gopher"},
                {79,"finger"},
                {80,"http"},
                {81,"hosts2-ns"},
                {88,"kerberos"},
                {101,"hostname"},
                {102,"iso-tsap"},
                {107,"rtelnet"},
                {109,"pop2"},
                {110,"pop3"},
                {111,"sunrpc"},
                {113,"auth"},
                {117,"uucp-path"},
                {118,"sqlserv"},
                {119,"nntp"},
                {123,"ntp"},
                {135,"epmap"},
                {137,"netbios-ns"},
                {138,"netbios-dgm"},
                {139,"netbios-ssn"},
                {143,"imap"},
                {150,"sql-net"},
                {156,"sqlsrv"},
                {158,"pcmail-srv"},
                {161,"snmp"},
                {162,"snmptrap"},
                {170,"print-srv"},
                {179,"bgp"},
                {194,"irc"},
                {213,"ipx"},
                {322,"rtsps"},
                {349,"mftp"},
                {389,"ldap"},
                {443,"https"},
                {445,"microsoft-ds"},
                {464,"kpasswd"},
                {500,"isakmp"},
                {507,"crs"},
                {512,"exec"},
                {513,"who"},
                {514,"syslog"},
                {515,"printer"},
                {517,"talk"},
                {518,"ntalk"},
                {520,"efs"},
                {522,"ulp"},
                {525,"timed"},
                {526,"tempo"},
                {529,"irc-serv"},
                {530,"courier"},
                {531,"conference"},
                {532,"netnews"},
                {533,"netwall"},
                {540,"uucp"},
                {543,"klogin"},
                {544,"kshell"},
                {546,"dhcpv6-client"},
                {547,"dhcpv6-server"},
                {548,"afpovertcp"},
                {550,"new-rwho"},
                {554,"rtsp"},
                {556,"remotefs"},
                {560,"rmonitor"},
                {561,"monitor"},
                {563,"nntps"},
                {565,"whoami"},
                {568,"ms-shuttle"},
                {569,"ms-rome"},
                {593,"http-rpc-epmap"},
                {612,"hmmp-ind"},
                {613,"hmmp-op"},
                {636,"ldaps"},
                {666,"doom"},
                {691,"msexch-routing"},
                {749,"kerberos-adm"},
                {750,"kerberos-iv"},
                {800,"mdbs_daemon"},
                {989,"ftps-data"},
                {990,"ftps"},
                {992,"telnets"},
                {993,"imaps"},
                {994,"ircs"},
                {995,"pop3s"},
                {1109,"kpop"},
                {1110,"nfsd-status"},
                {1155,"nfa"},
                {1034,"activesync"},
                {1167,"phone"},
                {1270,"opsmgr"},
                {1433,"ms-sql-s"},
                {1434,"ms-sql-m"},
                {1477,"ms-sna-server"},
                {1478,"ms-sna-base"},
                {1512,"wins"},
                {1524,"ingreslock"},
                {1607,"stt"},
                {1701,"l2tp"},
                {1711,"pptconference"},
                {1723,"pptp"},
                {1731,"msiccp"},
                {1745,"remote-winsock"},
                {1755,"ms-streaming"},
                {1801,"msmq"},
                {1812,"radius"},
                {1813,"radacct"},
                {1863,"msnp"},
                {1900,"ssdp"},
                {1944,"close-combat"},
                {2049,"nfsd"},
                {2053,"knetd"},
                {2106,"mzap"},
                {2177,"qwave"},
                {2234,"directplay"},
                {2382,"ms-olap3"},
                {2393,"ms-olap1"},
                {2394,"ms-olap2"},
                {2460,"ms-theater"},
                {2504,"wlbs"},
                {2525,"ms-v-worlds"},
                {2701,"sms-rcinfo"},
                {2702,"sms-xfer"},
                {2703,"sms-chat"},
                {2704,"sms-remctrl"},
                {2725,"msolap-ptp2"},
                {2869,"icslap"},
                {3000,"rubyrails"},
                {3020,"cifs"},
                {3074,"xbox"},
                {3126,"ms-dotnetster"},
                {3132,"ms-rule-engine"},
                {3268,"msft-gc"},
                {3269,"msft-gc-ssl"},
                {3343,"ms-cluster-net"},
                {3389,"ms-rdp"},
                {3535,"ms-la"},
                {3540,"pnrp-port"},
                {3544,"teredo"},
                {3587,"p2pgroup"},
                {3702,"ws-discovery"},
                {3776,"dvcprov-port"},
                {3847,"msfw-control"},
                {3882,"msdts1"},
                {3935,"sdp-portmapper"},
                {4350,"net-device"},
                {4500,"ipsec-msft"},
                {4567,"sinatra"},
                {5355,"llmnr"},
                {5358,"wsd"},
                {5678,"rrac"},
                {5679,"dccm"},
                {5720,"ms-licensing"},
                {5500,"vnc"},
                {5800,"vnc"},
                {5801,"vnc"},
                {5900,"vnc"},
                {5901,"vnc"},
                {6073,"directplay8"},
                {9535,"man"},
                {9753,"rasadv"},
                {10000,"webmin"},
                {11320,"imip-channels"},
                {47624,"directplaysrvr"},
            };

        #endregion

    }
}
