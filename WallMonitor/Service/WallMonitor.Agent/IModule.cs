namespace WallMonitor.Agent;

public interface IModule
{
    /// <summary>
    /// Name of module
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Get the current value
    /// </summary>
    string CurrentValue { get; }

    /// <summary>
    /// Get the data value
    /// </summary>
    double Value { get; }

    /// <summary>
    /// Error code
    /// </summary>
    int ErrorCode { get; }

    /// <summary>
    /// Get the current dictionary of values
    /// </summary>
    IDictionary<string, long> CurrentDictionary { get; }

    /// <summary>
    /// Dispose of internal resources
    /// </summary>
    void Dispose();
}