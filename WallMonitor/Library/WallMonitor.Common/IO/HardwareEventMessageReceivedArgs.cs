using System.Net;
using WallMonitor.Common.IO.AgentMessages;

namespace WallMonitor.Common.IO;

public class HardwareEventMessageReceivedArgs : EventArgs
{
    /// <summary>
    /// Hardware information data
    /// </summary>
    public HardwareInformationMessage EventData { get; set; }
    
    /// <summary>
    /// The remote endpoint that sent the data
    /// </summary>
    public string RemoteEndPoint { get; set; }

    public HardwareEventMessageReceivedArgs(HardwareInformationMessage eventData, string remoteEndPoint)
    {

        EventData = eventData;
        RemoteEndPoint = remoteEndPoint;
    }
}