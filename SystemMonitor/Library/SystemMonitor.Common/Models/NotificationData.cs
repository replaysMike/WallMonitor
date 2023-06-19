using SystemMonitor.Common.Abstract;

namespace SystemMonitor.Common.Models
{
    /// <summary>
    /// An object that contains data required to create a notification message
    /// </summary>
    public class NotificationData
	{
		public ISchedule Schedule { get; set; }
		public bool IsUp { get; set; }
		public string BroadcastMessage { get; set; }
		public string IncidentNumber { get; set; }
		public DateTime DateCreated { get; set; } = DateTime.UtcNow;
		public bool IsSent { get; set; }

		// the following code maintains an always incrementing incident number for lifetime of application
		private static readonly object IncidentNumberLock = new object();
		internal int IncidentNumberTracker = 0;
		internal DateTime LastIncidentDate = DateTime.MinValue;
		private static NotificationData? _instance = null;
		public static NotificationData Instance
		{
			get
			{
				lock (IncidentNumberLock)
				{
					if (_instance == null)
						_instance = new NotificationData();
					return _instance;
				}
			}
		}

		public NotificationData() {}

		public NotificationData(ISchedule schedule, bool isUp, string broadcastMessage)
		{
			Schedule = schedule;
			IsUp = isUp;
			BroadcastMessage = broadcastMessage;
			IncidentNumber = "";
		}

		/// <summary>
		/// Get the current incident number in memory
		/// </summary>
		/// <returns></returns>
		public static string GetCurrentIncidentNumber()
		{
			var incidentNumber = NotificationData.Instance.IncidentNumberTracker;
			var str = "";
			var now = NotificationData.Instance.LastIncidentDate;
			str = $"{now.Year}{now.Month}{now.Day}-{incidentNumber:D4}";
			return str;
		}

		/// <summary>
		/// Generate a new incident number and persist it
		/// </summary>
		/// <returns></returns>
		public static string GenerateIncidentNumber()
		{
			var incidentNumber = System.Threading.Interlocked.Increment(ref NotificationData.Instance.IncidentNumberTracker);
			var now = DateTime.UtcNow;
			// if the date changed, reset the incident number
			if (now.Year != NotificationData.Instance.LastIncidentDate.Year || now.Month != NotificationData.Instance.LastIncidentDate.Month || now.Day != NotificationData.Instance.LastIncidentDate.Day)
				NotificationData.Instance.IncidentNumberTracker = 1;
			var str = $"{now.Year}{now.Month}{now.Day}-{incidentNumber:D4}";
			NotificationData.Instance.LastIncidentDate = now;
			return str;
		}
	}
}
