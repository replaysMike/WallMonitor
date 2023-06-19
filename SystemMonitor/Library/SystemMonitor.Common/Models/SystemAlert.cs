namespace SystemMonitor.Common.Models
{
    public class SystemAlert
	{
		public SystemAlertLevel AlertLevel;
		public string Message;
		public string SenderName;
		public int TimeToLive = 3000;
		public bool DisableAudioCount = false;
		public Guid AlertId { get; set; }

		public SystemAlert()
		{
			AlertId = Guid.NewGuid();
		}

		public override bool Equals(object obj)
		{
			return GetHashCode().Equals(obj.GetHashCode());
		}
		public override int GetHashCode()
		{
			var hashCode = 7;
			hashCode = hashCode * 31 + (int)AlertLevel;
			hashCode = hashCode * 31 + Message.GetHashCode();
			return hashCode;
		}
	}
}
