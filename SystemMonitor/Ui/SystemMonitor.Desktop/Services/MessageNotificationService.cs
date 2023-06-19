using System;

namespace SystemMonitor.Desktop.Services
{
    /// <summary>
    /// Receives messages about server updates
    /// </summary>
    public class MessageNotificationService : IMessageNotificationService
    {
        public delegate void ServerMessageReceivedHandler(object sender, ServerMessageEventArgs e);
        public delegate void MonitoringServiceMessageReceivedHandler(object sender, MonitoringServiceEventArgs e);
        public event ServerMessageReceivedHandler? OnReceiveServerMessage;
        public event MonitoringServiceMessageReceivedHandler? OnReceiveMonitoringServiceMessage;

        public MessageNotificationService()
        {
        }

        public void SendServerMessage(ServerMessage message)
        {
            OnReceiveServerMessage?.Invoke(this, new ServerMessageEventArgs(message));
        }

        public void SendMonitoringServiceMessage(ServiceUpdateMessage message)
        {
            OnReceiveMonitoringServiceMessage?.Invoke(this, new MonitoringServiceEventArgs(message));
        }
    }

    public class ServerMessageEventArgs : EventArgs
    {
        public ServerMessage Message { get; set; }

        public ServerMessageEventArgs(ServerMessage message)
        {
            Message = message;
        }
    }

    public class MonitoringServiceEventArgs : EventArgs
    {
        public ServiceUpdateMessage Message { get; set; }

        public MonitoringServiceEventArgs(ServiceUpdateMessage message)
        {
            Message = message;
        }
    }
}
