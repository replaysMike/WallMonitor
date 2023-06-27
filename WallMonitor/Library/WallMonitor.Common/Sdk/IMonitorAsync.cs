namespace WallMonitor.Common.Sdk
{
    /// <summary>
    /// Network monitoring plugin asynchronously
    /// </summary>
    public interface IMonitorAsync : IMonitorBase, IDisposable
    {
        /// <summary>
		/// Check a host if it is available
		/// </summary>
		/// <param name="host">The host to communicate with</param>
		/// <param name="parameters">Configuration parameters required by the monitor</param>
		/// <param name="cancelToken">Cancellation token to cancel the asynchronous task</param>
		/// <returns></returns>
		Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, CancellationToken cancelToken);

        /// <summary>
        /// Generate a configuration template
        /// </summary>
        /// <returns></returns>
        object GenerateConfigurationTemplate();
    }
}
