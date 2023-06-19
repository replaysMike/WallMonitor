namespace SystemMonitor.Common.Models
{
    public class DateRange
	{
		/// <summary>
		/// The days of the week bitmask, based on DayOfWeek flags.
		/// </summary>
		public int DaysOfWeek { get; set; }

        /// <summary>
        /// The time in which to start
        /// </summary>
        public Time StartTime { get; set; } = null!;
		/// <summary>
		/// The time in which to end
		/// </summary>
		public Time EndTime { get; set; } = null!;
	}
}
