namespace SystemMonitor.Common.IO;

/// <summary>
/// Server notification event type
/// </summary>
public enum EventType
{
    /// <summary>
    /// Started to check a service on a host
    /// </summary>
    HostCheckStarted = 0,
    /// <summary>
    /// Completed check of a service on a host
    /// </summary>
    HostCheckCompleted,
    /// <summary>
    /// Host service check failed
    /// </summary>
    HostCheckFailed,
    /// <summary>
    /// Host service check recovered from failure
    /// </summary>
    HostCheckRecovered,
    /// <summary>
    /// Monitoring service is offline
    /// </summary>
    ServiceOffline,
    /// <summary>
    /// Monitoring service restored
    /// </summary>
    ServiceRestored
}