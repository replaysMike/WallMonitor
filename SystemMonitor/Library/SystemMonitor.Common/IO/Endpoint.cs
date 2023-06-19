namespace SystemMonitor.Common.IO
{
    public class Endpoint
    {
        public string? Name { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }

        public Endpoint(string? name, string ip, int port)
        {
            Name = name;
            Ip = ip;
            Port = port;
        }
    }
}
