using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using Microsoft.Extensions.Logging;
using System.Net;
using WallMonitor.Common.Abstract;
using WallMonitor.Common.IO;

namespace WallMonitor.Common.Notifications
{
    public class SnmpService : ISnmpService, INotificationRecipientService
    {
        private readonly string _defaultEngineId = "80001F8880E5630000B61FF450";
        private readonly ILogger<SnmpService> _logger;
        private readonly SnmpConfiguration _configuration;
        private readonly IPAddress _managementServer;
        private int _requestId;
        private int _messageId;
        // via: https://oidref.com/1.3.6.1.2.1.1.5
        private readonly Dictionary<OidType, string> _oids = new ()
        {
            { OidType.HostName, "1.3.6.1.2.1.1.5.0" },
            { OidType.ServiceName, "1.3.6.1.2.1.1.1.0" },
            { OidType.AttemptsCount, "1.3.6.1.2.1.6.7" }
        };

        public enum OidType
        {
            HostName,
            ServiceName,
            AttemptsCount
        }

        public SnmpService(ILogger<SnmpService> logger, SnmpConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _managementServer = IPAddress.Parse(_configuration.ManagementServer);
        }

        public async Task<bool> SendMessageAsync(string recipient, EventType eventType, ServiceState serviceState, ISchedule schedule, bool isEscalated)
        {
            if (!_configuration.Enabled) return false;

            _requestId++;
            _messageId++;

            switch (_configuration.Version)
            {
                case SnmpVersion.V1:
                    return await SendTrapV1Async(eventType, serviceState, schedule);
                default:
                case SnmpVersion.V2:
                    return await SendTrapV2Async(VersionCode.V2, eventType, serviceState, schedule);
                case SnmpVersion.V2U:
                    return await SendTrapV2Async(VersionCode.V2U, eventType, serviceState, schedule);
                case SnmpVersion.V3:
                    return await SendTrapV3Async(eventType, serviceState, schedule);
            }
        }

        private async Task<bool> SendTrapV1Async(EventType eventType, ServiceState serviceState, ISchedule schedule)
        {
            try
            {
                var genericCode = GenericCode.ColdStart;
                var specificCode = 0;
                if (serviceState == ServiceState.Up)
                {
                    genericCode = GenericCode.LinkUp;
                    specificCode = 0;
                }

                if (serviceState == ServiceState.Down)
                {
                    genericCode = GenericCode.LinkDown;
                    specificCode = 1;
                }

                if (serviceState == ServiceState.Error)
                {
                    genericCode = GenericCode.EgpNeighborLoss;
                    specificCode = 2;
                }

                var agent = (await Dns.GetHostEntryAsync(Dns.GetHostName()))
                            .AddressList
                            .FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            ?? IPAddress.Loopback;

                await Messenger.SendTrapV1Async(
                    new IPEndPoint(_managementServer, _configuration.Port),
                    agent: agent,
                    new OctetString(_configuration.Community),
                    new ObjectIdentifier(GetOidFromState(serviceState)),
                    genericCode,
                    specific: specificCode,
                    timestamp: (uint)DateTime.UtcNow.Ticks,
                    variables: GetIdentifierVariables(schedule));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(SnmpService)}");
                return false;
            }

            return true;
        }

        private async Task<bool> SendTrapV2Async(VersionCode versionCode, EventType eventType, ServiceState serviceState, ISchedule schedule)
        {
            try
            {
                await Messenger.SendTrapV2Async(
                    requestId: _requestId,
                    versionCode,
                    new IPEndPoint(_managementServer, _configuration.Port),
                    // community string
                    new OctetString(_configuration.Community),
                    new ObjectIdentifier(GetOidFromState(serviceState)),
                    timestamp: (uint)DateTime.UtcNow.Ticks,
                    variables: GetIdentifierVariables(schedule));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(SnmpService)}");
                return false;
            }
            return true;
        }

        private async Task<bool> SendTrapV3Async(EventType eventType, ServiceState serviceState, ISchedule schedule)
        {
            try
            {
                if (!string.IsNullOrEmpty(_configuration.PrivacyPassword))
                {
                    // encrypted
                    IPrivacyProvider? privacyProvider = null;
                    IAuthenticationProvider? authenticationProvider = null;
                    switch (_configuration.AuthenticationAlgorithm?.ToLower())
                    {
                        case "md5":
                            authenticationProvider = new MD5AuthenticationProvider(new OctetString(_configuration.PrivacyPassword));
                            break;
                        case "sha1":
                            authenticationProvider = new SHA1AuthenticationProvider(new OctetString(_configuration.PrivacyPassword));
                            break;
                        case "sha256":
                            authenticationProvider = new SHA256AuthenticationProvider(new OctetString(_configuration.PrivacyPassword));
                            break;
                        case "sha384":
                            authenticationProvider = new SHA384AuthenticationProvider(new OctetString(_configuration.PrivacyPassword));
                            break;
                        case "sha512":
                            authenticationProvider = new SHA512AuthenticationProvider(new OctetString(_configuration.PrivacyPassword));
                            break;
                    }

                    if (authenticationProvider == null && !string.IsNullOrEmpty(_configuration.PrivacyAlgorithm))
                        throw new InvalidOperationException($"Privacy algorithm of '' was specified, however no authentication algorithm was specified!");

                    switch (_configuration.PrivacyAlgorithm?.ToLower())
                    {
                        case "des":
                            privacyProvider = new DESPrivacyProvider(new OctetString(_configuration.AuthenticationPassword), authenticationProvider);
                            break;
                        case "tripledes":
                            privacyProvider = new TripleDESPrivacyProvider(new OctetString(_configuration.AuthenticationPassword), authenticationProvider);
                            break;
                        case "aes":
                            privacyProvider = new AESPrivacyProvider(new OctetString(_configuration.AuthenticationPassword), authenticationProvider);
                            break;
                        case "aes192":
                            privacyProvider = new AES192PrivacyProvider(new OctetString(_configuration.AuthenticationPassword), authenticationProvider);
                            break;
                        case "aes256":
                            privacyProvider = new AES256PrivacyProvider(new OctetString(_configuration.AuthenticationPassword), authenticationProvider);
                            break;
                    }

                    var trap = new TrapV2Message(
                        VersionCode.V3,
                        messageId: _messageId,
                        requestId: _requestId,
                        userName: new OctetString(_configuration.Username),
                        enterprise: new ObjectIdentifier(GetOidFromState(serviceState)),
                        time: (uint)DateTime.UtcNow.Ticks,
                        variables: GetIdentifierVariables(schedule),
                        privacy: privacyProvider,
                        maxMessageSize: 0x10000,
                        engineId: new OctetString(ByteTool.Convert(!string.IsNullOrEmpty(_configuration.SnmpV3EngineId) ? _configuration.SnmpV3EngineId : _defaultEngineId)),
                        engineBoots: 0,
                        engineTime: 0);
                    await trap.SendAsync(new IPEndPoint(_managementServer, _configuration.Port));
                }
                else
                {
                    // unecnrypted
                    var trap = new TrapV2Message(
                        VersionCode.V3,
                        messageId: _messageId,
                        requestId: _requestId,
                        userName: new OctetString(_configuration.Username),
                        enterprise: new ObjectIdentifier(GetOidFromState(serviceState)),
                        time: (uint)DateTime.UtcNow.Ticks,
                        variables: GetIdentifierVariables(schedule),
                        privacy: DefaultPrivacyProvider.DefaultPair,
                        maxMessageSize: 0x10000,
                        engineId: new OctetString(ByteTool.Convert(!string.IsNullOrEmpty(_configuration.SnmpV3EngineId) ? _configuration.SnmpV3EngineId : _defaultEngineId)),
                        engineBoots: 0,
                        engineTime: 0);
                    await trap.SendAsync(new IPEndPoint(_managementServer, _configuration.Port));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(SnmpService)}");
                return false;
            }
            return true;
        }

        private string GetOidFromState(ServiceState serviceState)
        {
            // https://oidref.com/1.3.6.1.6.3.1.1.5
            var oidLinkUnknown = $"{_configuration.Oid}.1";
            var oidLinkDown = $"{_configuration.Oid}.3";
            var oidLinkUp = $"{_configuration.Oid}.4";
            var oidLinkError = $"{_configuration.Oid}.6";
            var oid = oidLinkUp;
            switch (serviceState)
            {
                case ServiceState.Down:
                    oid = oidLinkDown;
                    break;
                case ServiceState.Up:
                    oid = oidLinkUp;
                    break;
                case ServiceState.Unknown:
                    oid = oidLinkUnknown;
                    break;
                case ServiceState.Error:
                    oid = oidLinkError;
                    break;
            }
            return oid;
        }

        private List<Variable> GetIdentifierVariables(ISchedule schedule)
        {
            return new List<Variable>()
            {
                new(new ObjectIdentifier(_oids[OidType.HostName]), new OctetString(schedule.Host?.Name ?? "Unknown")),
                new(new ObjectIdentifier(_oids[OidType.ServiceName]), new OctetString(schedule.Name)),
                new(new ObjectIdentifier(_oids[OidType.AttemptsCount]), new Integer32(schedule.ConsecutiveAttempts)),
            };
        }
    }
}
