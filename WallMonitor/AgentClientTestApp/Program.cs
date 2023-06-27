// See https://aka.ms/new-console-template for more information

using WallMonitor.Common.IO;
using WallMonitor.Common.IO.Messages;

using var client = new TcpAgentClient("localhost", "127.0.0.1", 3500, EncryptionTypes.Unencrypted, string.Empty, new List<WallMonitor.Common.ServiceConfiguration>());

await client.ConnectAsync();

