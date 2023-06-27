using System.Net;

namespace WallMonitor.Common.IO.Server.Transport;

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