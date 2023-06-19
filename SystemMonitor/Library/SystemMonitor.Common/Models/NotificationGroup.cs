namespace SystemMonitor.Common.Models
{
    /// <summary>
    /// A notification group of people
    /// </summary>
    public class NotificationGroup
	{
		public string Name { get; set; }
		public List<NotificationGroupMember> Members { get; set; }
		public bool Enabled { get; set; }
		public int MaxRepeatIntervalSeconds { get; set; }
		public int MinAlertThreshold { get; set; }

		public NotificationGroup(string name, List<NotificationGroupMember> members, int maxRepeatIntervalSeconds, int minAlertThreshold)
		{
			Enabled = true;
			Name = name;
			Members = members;
			MaxRepeatIntervalSeconds = maxRepeatIntervalSeconds;
			MinAlertThreshold = minAlertThreshold;
		}
		public NotificationGroup(string name, List<NotificationGroupMember> members, bool enabled, int maxRepeatIntervalSeconds, int minAlertThreshold)
		{
			Enabled = enabled;
			Name = name;
			Members = members;
			MaxRepeatIntervalSeconds = maxRepeatIntervalSeconds;
			MinAlertThreshold = minAlertThreshold;
		}
	}
}
