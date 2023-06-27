namespace WallMonitor.Common
{
    public enum Units : byte
    {
        Auto = 0,
        /// <summary>
        /// Percentage value
        /// </summary>
        Percentage,
        /// <summary>
        /// Raw value
        /// </summary>
        Value,
        /// <summary>
        /// Time value in milliseconds
        /// </summary>
        Time,
        /// <summary>
        /// Bytes value
        /// </summary>
        Bytes,
        Kb,
        Mb,
        Gb,
        Tb,
        Pb,
    }
}
