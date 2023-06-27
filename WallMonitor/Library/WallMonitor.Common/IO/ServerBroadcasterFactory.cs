using WallMonitor.Common.IO.Messages;
using WallMonitor.Common.IO.Security;
using WallMonitor.Common.IO.Server;

namespace WallMonitor.Common.IO
{
    public class ServerBroadcasterFactory
    {
        private readonly UdpBroadcasterConfiguration _udpConfiguration;
        private readonly TcpServerConfiguration _tcpConfiguration;
        private readonly LimitedConfiguration _limitedConfiguration;
        private readonly EncryptionTypes _encryptionType;
        private readonly IAesEncryptionService _aesStringEncryptionService;
        private readonly string? _encryptionKey;

        public ServerBroadcasterFactory(UdpBroadcasterConfiguration udpConfiguration, TcpServerConfiguration tcpConfiguration, LimitedConfiguration limitedConfiguration, EncryptionTypes encryptionType, string? encryptionKey, IAesEncryptionService aesStringEncryptionService)
        {
            _udpConfiguration = udpConfiguration;
            _tcpConfiguration = tcpConfiguration;
            _limitedConfiguration = limitedConfiguration;
            _encryptionType = encryptionType;
            _encryptionKey = encryptionKey;
            _aesStringEncryptionService = aesStringEncryptionService;
        }

        public IBroadcaster Create(ServerType serverType)
        {
            switch (serverType)
            {
                case ServerType.UdpMulticast:
                    return new UdpBroadcaster(_udpConfiguration, _limitedConfiguration, _encryptionType, _encryptionKey, _aesStringEncryptionService);
                case ServerType.Tcp:
                    return new TcpServerBroadcaster(_limitedConfiguration, _tcpConfiguration, _encryptionType, _encryptionKey, _aesStringEncryptionService);
            }

            throw new NotSupportedException($"Unknown server type {serverType}!");
        }
    }
}
