using static WallMonitor.Desktop.Services.MessageNotificationService;

namespace WallMonitor.Desktop.Services;

public interface IMessageNotificationService
{
    /// <summary>
    /// Event to invoke when a message is received
    /// </summary>
    event ServerMessageReceivedHandler OnReceiveServerMessage;

    /// <summary>
    /// Event to invoke when a message is received
    /// </summary>
    event MonitoringServiceMessageReceivedHandler OnReceiveMonitoringServiceMessage;

    void SendServerMessage(ServerMessage message);
    void SendMonitoringServiceMessage(ServiceUpdateMessage message);
}