using SystemMonitor.Common.Abstract;

namespace SystemMonitor.Common.Models
{
    public class ScheduleTime : IScheduleTime
	{
		public DateRange DateRange { get; set; }
		public TimeSpan Interval { get; set; }
		public ScheduleTime() { }
		public ScheduleTime(TimeSpan interval)
		{
			Interval = interval;
		}
		public ScheduleTime(DateRange dateRange, TimeSpan interval)
		{
			DateRange = dateRange;
			Interval = interval;
		}
	}
}
