using System.Net.Sockets;

namespace WallMonitor.Common.IO.Server.Transport;

public class ConnectionRejectedEventArgs : EventArgs
{
    public Socket Socket { get; set; }

    public ConnectionRejectedEventArgs(Socket socket)
    {
        Socket = socket;
    }
}