using System;
using WallMonitor.Common.IO;

namespace WallMonitor.Desktop.Services
{
    /// <summary>
    /// Server message
    /// </summary>
    /// <param name="MonitorId"></param>
    /// <param name="EventType"></param>
    /// <param name="Message"></param>
    public record ServerMessage(Guid MonitorId, EventType EventType, string Message);
}
