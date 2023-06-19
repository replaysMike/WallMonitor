namespace SystemMonitor.Common.Models
{
    public class SystemAlertEventArgs : EventArgs
	{
		public SystemAlert Alert = new SystemAlert();

		public SystemAlertEventArgs() { }

		public SystemAlertEventArgs(SystemAlertLevel alertLevel, string message, string senderName, bool disableAudioCount = false)
		{
			Alert.AlertLevel = alertLevel;
			Alert.Message = message;
			Alert.SenderName = senderName;
			Alert.DisableAudioCount = disableAudioCount;
		}
		public SystemAlertEventArgs(SystemAlertLevel alertLevel, string message, string senderName, int ttl)
		{
			Alert.AlertLevel = alertLevel;
			Alert.Message = message;
			Alert.SenderName = senderName;
			Alert.TimeToLive = ttl;
		}
	}
}
