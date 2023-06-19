using SystemMonitor.Common.Abstract;

namespace SystemMonitor.Common.Models
{
    /// <summary>
    /// A notification message
    /// </summary>
    public class NotificationMessage
	{
		public string Message { get; set; }
		public string HTMLMessage { get; set; }
		public string Subject { get; set; }
		public List<NotificationGroup> Recipients { get; set; }
		public ISchedule Schedule { get; set; }
		public bool UpState { get; set; }
		public DateTime DateCreated { get; set; }
		private Dictionary<string, string> Tags { get; set; }

		public NotificationMessage()
		{
			Recipients = new List<NotificationGroup>();
			DateCreated = DateTime.UtcNow;
		}
		public NotificationMessage(List<NotificationGroup> recipients)
		{
			Recipients = recipients;
			DateCreated = DateTime.UtcNow;
		}
		public NotificationMessage(string message, string htmlMessage, List<NotificationGroup> recipients)
		{
			Message = message;
			HTMLMessage = htmlMessage;
			Recipients = recipients;
			DateCreated = DateTime.UtcNow;
		}
		public NotificationMessage(string message, string htmlMessage, string subject, List<NotificationGroup> recipients)
		{
			Message = message;
			HTMLMessage = htmlMessage;
			Subject = subject;
			Recipients = recipients;
			DateCreated = DateTime.UtcNow;
		}
		public NotificationMessage(string message, string htmlMessage, string subject, List<NotificationGroup> recipients, ISchedule schedule, bool upState)
		{
			Message = message;
			HTMLMessage = htmlMessage;
			Subject = subject;
			Recipients = recipients;
			Schedule = schedule;
			DateCreated = DateTime.UtcNow;
			UpState = upState;
		}

		
	}
}
