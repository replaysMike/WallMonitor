using WallMonitor.Common.Abstract;

namespace WallMonitor.Common.Models
{
    public class HostEventArgs : EventArgs
	{
		public ISchedule Schedule { get; set; }

		public HostEventArgs(ISchedule schedule)
		{
			Schedule = schedule;
		}
	}
}
