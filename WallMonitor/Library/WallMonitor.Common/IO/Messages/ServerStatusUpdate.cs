namespace WallMonitor.Common.IO.Messages
{
    /// <summary>
    /// Server status update message
    /// </summary>
    /// <param name="MessageVersion">Message version for supporting schema updates</param>
    /// <param name="EventType"></param>
    /// <param name="Host">Name of host</param>
    /// <param name="HostId">Unique host id</param>
    /// <param name="MonitorId">Unique monitor id</param>
    /// <param name="Service"></param>
    /// <param name="DateTime"></param>
    /// <param name="ServiceState"></param>
    /// <param name="LastUpTime"></param>
    /// <param name="PreviousDownTime">The length of the previous down time</param>
    /// <param name="Value"></param>
    /// <param name="Range"></param>
    /// <param name="Units"></param>
    /// <param name="ResponseTime"></param>
    /// <param name="GraphType"></param>
    public record ServerStatusUpdate(byte MessageVersion, EventType EventType, string Host, int HostId, int MonitorId, string Service, DateTime DateTime, ServiceState ServiceState, DateTime LastUpTime, TimeSpan PreviousDownTime, double? Value, string? Range, Units Units, TimeSpan ResponseTime, GraphType GraphType);
}
