namespace SystemMonitor.Common.Abstract
{
    public interface IGraphData
	{
		/// <summary>
		/// Graph data
		/// </summary>
		ICollection<double> Data { get; set; }

		/// <summary>
		/// Frequency of update
		/// </summary>
		TimeSpan Frequency { get; set; }

		/// <summary>
		/// Last data entry
		/// </summary>
		DateTime LastDataEntry { get; set; }
	}
}
