using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Common.Abstract
{
    public interface ISchedule
    {
        /// <summary>
        /// A unique identifier for the schedule.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// A unique identifier for the host
        /// </summary>
        int HostId { get; }

        /// <summary>
        /// A unique id for the monitor
        /// </summary>
        int MonitorId { get; }

        TimeSpan InitDelay { get; set; }

        /// <summary>
		/// A list of scheduled times
		/// </summary>
		IList<IScheduleTime> Times { get; set; }

        /// <summary>
        /// The asynchronous monitor to run
        /// </summary>
        IMonitorAsync MonitorAsync { get; set; }

        /// <summary>
        /// The configuration parameters for the monitor
        /// </summary>
        IConfigurationParameters? ConfigurationParameters { get; set; }

        /// <summary>
        /// The name of this schedule
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The last value returned by the monitor
        /// </summary>
        double? Value { get; set; }

        /// <summary>
        /// The range value used for improving graphing scales
        /// </summary>
        string? Range { get; set; }

        /// <summary>
        /// The units the value should be displayed in
        /// </summary>
        Units Units { get; set; }

        /// <summary>
        /// Response time
        /// </summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// The host to check
        /// </summary>
        IHost? Host { get; set; }

        /// <summary>
        /// The time this monitor was last run
        /// </summary>
        DateTime LastRuntime { get; set; }

        /// <summary>
        /// A copy of the last response received from the host, for quick access.
        /// </summary>
        IHostResponse? LastResponse { get; set; }

        /// <summary>
        /// The complete response history for this schedule
        /// </summary>
        HashSet<IHostResponse>? ResponseHistory { get; set; }

        /// <summary>
        /// The number of failed attempts to trigger an alert on
        /// </summary>
        int Attempts { get; }

        /// <summary>
        /// The timeout to use
        /// </summary>
        TimeSpan Timeout { get; }

        /// <summary>
        /// Get the uptime percentage
        /// </summary>
        double UpTime { get; }

        /// <summary>
        /// The date of the last successful response
        /// </summary>
        DateTime LastUpTime { get; }

        /// <summary>
        /// The length of time the service has been down for previously
        /// </summary>
        TimeSpan PreviousDownTime { get; }

        /// <summary>
        /// Get the health state - from 0% to 100%
        /// </summary>
        double HealthStatus { get; }

        /// <summary>
        /// Get if a server is currently flapping (constant up/down messages)
        /// </summary>
        bool IsFlapping { get; }

        /// <summary>
        /// Get if the service is currently in up state
        /// </summary>
        bool IsUp { get; }

        /// <summary>
        /// True to enable sending of notifications on this schedule
        /// </summary>
        bool Notify { get; set; }

        /// <summary>
        /// The number of consecutive failed attempts
        /// </summary>
        int ConsecutiveAttempts { get; set; }

        /// <summary>
        /// Get raw data from a schedule about its history, sorted by Date.
        /// </summary>
        /// <returns></returns>
        IGraphData? ToGraphData(int recordCount = 500);

        /// <summary>
        /// Add a response object to the schedule's history
        /// </summary>
        /// <param name="response"></param>
        void AddResponseHistory(IHostResponse response);

        /// <summary>
        /// Clear the schedule's history data
        /// </summary>
        void Clear();

        /// <summary>
        /// Count the response history items
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        int Count(Func<IHostResponse, bool> match);

        /// <summary>
        /// Remove response history items that match expression
        /// </summary>
        /// <param name="match"></param>
        void RemoveWhere(Predicate<IHostResponse> match);
    }
}
