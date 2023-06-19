namespace SystemMonitor.Common.Sdk
{
    public interface IConfigurationParameter
	{
		/// <summary>
		/// Name of configuration value
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Value
		/// </summary>
		object? Value { get; set; }
    }
}
