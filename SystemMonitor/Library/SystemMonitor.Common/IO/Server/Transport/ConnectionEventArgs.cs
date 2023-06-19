using System.Net;

namespace SystemMonitor.Common.IO.Server.Transport;

public class ConnectionEventArgs : EventArgs
{
    public EndPoint RemoteEndPoint { get; set; }
    public Guid ConnectionId { get; set; }

    public ConnectionEventArgs(EndPoint remoteEndPoint, Guid connectionId)
    {
        RemoteEndPoint = remoteEndPoint;
        ConnectionId = connectionId;
    }
}