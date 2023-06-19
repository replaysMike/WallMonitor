namespace SystemMonitor.Common.Sdk
{
    public interface IHostResponse
    {
        /// <summary>
        /// Date last checked
        /// </summary>
        DateTime DateChecked { get; set; }

        /// <summary>
        /// Last response time duration
        /// </summary>
        TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// Returns true if server is up
        /// </summary>
        bool IsUp { get; set; }

        /// <summary>
        /// State object
        /// </summary>
        object? State { get; set; }

        /// <summary>
        /// Last value returned by monitor
        /// </summary>
        double? Value { get; set; }

        /// <summary>
        /// The min/max range of values possible for Value.
        /// Used for establishing better scales for graphing
        /// </summary>
        string? Range { get; set; }

        /// <summary>
        /// The units the value should be displayed in
        /// </summary>
        Units Units { get; set; }
    }
}
