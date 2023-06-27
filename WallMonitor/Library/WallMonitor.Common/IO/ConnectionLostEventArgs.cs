namespace WallMonitor.Common.IO
{
    public class ConnectionLostEventArgs
    {
        public string Name { get; set; }
        public Uri Uri { get; set; }

        public ConnectionLostEventArgs(string name, Uri uri)
        {
            Name = name;
            Uri = uri;
        }
    }
}
