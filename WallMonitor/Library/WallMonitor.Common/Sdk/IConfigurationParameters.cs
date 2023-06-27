namespace WallMonitor.Common.Sdk
{
    public interface IConfigurationParameters
	{
		HashSet<IConfigurationParameter> Parameters { get; set; }

		/// <summary>
		/// Check if the parameters contain a specific key value
		/// </summary>
		/// <param name="parameterName"></param>
		/// <returns></returns>
		bool Contains(string parameterName);
		
        /// <summary>
		/// Get value from config
		/// </summary>
		/// <param name="parameterName"></param>
		/// <returns></returns>
		dynamic? Get(string parameterName);

        /// <summary>
        /// Get value from config
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        dynamic? Get(string parameterName, dynamic? defaultValue);
		
        /// <summary>
		/// Get value from config
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parameterName"></param>
		/// <returns></returns>
		T Get<T>(string parameterName);

        /// <summary>
        /// Get value from config
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameterName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        T? Get<T>(string parameterName, T? defaultValue);

		/// <summary>
		/// Returns true if any parameters are defined
		/// </summary>
		/// <returns></returns>
        bool Any();
    }
}
