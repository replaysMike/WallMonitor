namespace SystemMonitor.Common.Models
{
    public enum MonitorCategory
    {
        /// <summary>
        /// Protocol specific monitor
        /// </summary>
        Protocol,
        /// <summary>
        /// Application specific service monitor
        /// </summary>
        Application,
        /// <summary>
        /// Database monitor
        /// </summary>
        Database,
        /// <summary>
        /// Windows only monitor
        /// </summary>
        Windows,
        /// <summary>
        /// Special category
        /// </summary>
        Special
    }
}
