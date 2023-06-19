using SystemMonitor.Common.Models;

namespace SystemMonitor.Common.Abstract
{
    public interface IScheduleTime
	{
		/// <summary>
		/// Date/time range to apply schedule
		/// </summary>
		DateRange DateRange { get; set; }

		/// <summary>
		/// The interval in which to check
		/// </summary>
		TimeSpan Interval { get; set; }
	}
}
