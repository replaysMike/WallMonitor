// See https://aka.ms/new-console-template for more information

using SystemMonitor.Common.IO;
using SystemMonitor.Common.IO.Messages;

using var client = new TcpAgentClient("localhost", "127.0.0.1", 3500, EncryptionTypes.Unencrypted, string.Empty, new List<SystemMonitor.Common.ServiceConfiguration>());

await client.ConnectAsync();

