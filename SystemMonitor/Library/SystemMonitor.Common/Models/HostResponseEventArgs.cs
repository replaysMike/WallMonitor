using SystemMonitor.Common.Abstract;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Common.Models
{
    public class HostResponseEventArgs : EventArgs
	{
		public ISchedule Schedule { get; set; }
		public IHostResponse HostResponse { get; set; }

		public HostResponseEventArgs(ISchedule schedule, IHostResponse hostResponse)
		{
			Schedule = schedule;
			HostResponse = hostResponse;
		}
	}
}
