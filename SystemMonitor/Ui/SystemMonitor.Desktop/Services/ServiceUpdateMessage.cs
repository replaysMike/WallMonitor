using System;
using SystemMonitor.Common;
using SystemMonitor.Common.IO;

namespace SystemMonitor.Desktop.Services;

/// <summary>
/// Service update message
/// </summary>
/// <param name="MonitorId"></param>
/// <param name="EventType"></param>
/// <param name="ServerName"></param>
/// <param name="ServiceName"></param>
/// <param name="Date">Utc date of the message</param>
/// <param name="State"></param>
/// <param name="Value"></param>
/// <param name="Range"></param>
/// <param name="Units"></param>
/// <param name="ResponseTime"></param>
/// <param name="LastUpTime">Indicates the last time the service was in the up state</param>
/// <param name="PreviousDownTime">Indicates how long a service was down for previously</param>
/// <param name="GraphType"></param>
public record ServiceUpdateMessage(Guid MonitorId, EventType EventType, string ServerName, string ServiceName, DateTime Date, ServiceState State, double? Value, string? Range, Units Units, TimeSpan ResponseTime, DateTime LastUpTime, TimeSpan PreviousDownTime, GraphType GraphType);