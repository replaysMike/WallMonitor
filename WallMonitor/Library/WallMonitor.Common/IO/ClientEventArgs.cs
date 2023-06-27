namespace WallMonitor.Common.IO
{
    public class ClientEventArgs : EventArgs
    {
        public string RemoteEndPoint { get; set; }

        public ClientEventArgs(string remoteEndPoint)
        {
            RemoteEndPoint = remoteEndPoint;
        }
    }
}
