namespace WallMonitor.Common
{
    /// <summary>
    /// The type of data to graph
    /// </summary>
    public enum GraphType : byte
    {
        /// <summary>
        /// Graph the value
        /// </summary>
        Value = 0,
        /// <summary>
        /// Graph the response time
        /// </summary>
        ResponseTime
    }
}
