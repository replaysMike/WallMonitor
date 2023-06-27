namespace WallMonitor.Common.Models
{
    [Flags]
	public enum NotificationTypes : int
	{
		None = 0,
		Email = 1,
		Phone = 2
	}
}
