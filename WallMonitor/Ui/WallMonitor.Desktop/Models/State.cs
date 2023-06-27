namespace WallMonitor.Desktop.Models;

// definition order affects UI sorting
public enum State
{
    /// <summary>
    /// Server is DOWN
    /// </summary>
    ServerDown = 0,
    /// <summary>
    /// Monitoring service is down
    /// </summary>
    ParentMonitoringServiceIsDown,
    /// <summary>
    /// Server is UP
    /// </summary>
    ServerIsRunning,
    /// <summary>
    /// Server has no state
    /// </summary>
    ServerIsNotRunning,
    /// <summary>
    /// Server is disabled and is not being checked or monitored
    /// </summary>
    ServerIsDisabled
}