using SystemMonitor.Common.Abstract;

namespace SystemMonitor.Common.Models
{
    public class GraphData : IGraphData
	{
		public ICollection<double> Data { get; set; } = new List<double>();
		public TimeSpan Frequency { get; set; }
		public DateTime LastDataEntry { get; set; }
	}
}
