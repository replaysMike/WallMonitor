namespace WallMonitor.Common.Notifications;

public class SnmpConfiguration
{
    public bool Enabled { get; set; }

    public string ManagementServer { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 162;
    public string Community { get; set; } = "public";
    public SnmpVersion Version { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Oid { get; set; } = string.Empty;
    public string PrivacyPassword { get; set; } = string.Empty;
    public string AuthenticationPassword { get; set; } = string.Empty;
    public string AuthenticationAlgorithm { get; set; } = string.Empty;
    public string PrivacyAlgorithm { get; set; } = string.Empty;
    public string SnmpV3EngineId { get; set; } = string.Empty;
}