namespace SystemMonitor.Common.IO
{
    public class ConnectionRestoredEventArgs
    {
        public string Name { get; set; }
        public Uri Uri { get; set; }

        public ConnectionRestoredEventArgs(string name, Uri uri)
        {
            Name = name;
            Uri = uri;
        }
    }
}
