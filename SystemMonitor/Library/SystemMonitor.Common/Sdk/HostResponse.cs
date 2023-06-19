namespace SystemMonitor.Common.Sdk
{
    public class HostResponse : IHostResponse
    {
        private HostResponse() { }

        public static IHostResponse Create()
        {
            return new HostResponse()
            {
                DateChecked = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Date last checked
        /// </summary>
        public DateTime DateChecked { get; set; }

        /// <summary>
        /// Last response time duration
        /// </summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// Returns true if server is up
        /// </summary>
        public bool IsUp { get; set; }

        /// <summary>
        /// State object
        /// </summary>
        public object? State { get; set; }

        /// <summary>
        /// Last value returned by monitor
        /// </summary>
        public double? Value { get; set; }

        /// <summary>
        /// The min/max range of values possible for Value.
        /// Used for establishing better scales for graphing
        /// </summary>
        public string? Range { get; set; }

        /// <summary>
        /// The units the value should be displayed in
        /// </summary>
        public Units Units { get; set; } = Units.Auto;
    }
}
