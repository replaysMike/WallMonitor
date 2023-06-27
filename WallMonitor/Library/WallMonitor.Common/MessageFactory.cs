using WallMonitor.Common.Abstract;
using WallMonitor.Common.Models;

namespace WallMonitor.Common
{
    public class MessageFactory
	{
		public string? BroadcastMessage { get; set; }

		public NotificationMessage CreateMessage(int templateId, ISchedule schedule, bool upState, MessageFormat messageFormat, Dictionary<string, string> customTags)
		{
			NotificationMessage message;
			switch (templateId)
			{
				default:
					message = GetGenericMessage(schedule, upState, messageFormat, customTags);
					break;
			}

			var rawMessage = message.Message;
			message.Message = ParseTemplateKeywords(rawMessage, false);
			message.HTMLMessage = ParseTemplateKeywords(rawMessage, true);

			return message;
		}

		public NotificationMessage CreateMessage(int templateId, ISchedule schedule, bool upState, MessageFormat messageFormat, string broadcastMessage, Dictionary<string, string> customTags)
		{
			BroadcastMessage = broadcastMessage;
			NotificationMessage message;
			switch (templateId)
			{
				default:
					message = GetGenericMessage(schedule, upState, messageFormat, customTags);
					break;
			}

			if (!string.IsNullOrEmpty(BroadcastMessage))
			{
				// if there is a broadcast message, send it with the message.
				message.Message += $"[NL][NL]*** NOTICE ***[NL][NL]{BroadcastMessage}[NL][NL]*** END NOTICE ***[NL]";
			}

			var rawMessage = message.Message;
			message.Message = ParseTemplateKeywords(rawMessage, false);
			message.HTMLMessage = ParseTemplateKeywords(rawMessage, true);

			return message;
		}

		public NotificationMessage GetGenericMessage(ISchedule schedule, bool upState, MessageFormat messageFormat, Dictionary<string, string> customTags)
		{
			var message = new NotificationMessage();
			message.Schedule = schedule;
			message.UpState = upState;
			var additionalData = "";
			additionalData += $"{schedule.MonitorAsync.ConfigurationDescription}[NL]";
			if (upState)
			{
				var downLength = "0 seconds";
				var lastUp = schedule.ResponseHistory.Where(x => x.IsUp).OrderByDescending(x => x.DateChecked).Select(x => x.DateChecked).FirstOrDefault();
				var firstDown = schedule.ResponseHistory.Where(x => !x.IsUp && x.DateChecked > lastUp).OrderBy(x => x.DateChecked).Select(x => x.DateChecked).FirstOrDefault();
				if (firstDown != null)
				{
					if (firstDown == DateTime.MinValue)
					{
						// there are no stats where this item has the required state (i.e. its always been down since app start)
						firstDown = System.Diagnostics.Process.GetCurrentProcess().StartTime;
					}

					var downTime = DateTime.UtcNow - firstDown;
					downLength = GetTimeMessage(downTime, messageFormat);
				}

				switch (messageFormat)
				{
					case MessageFormat.Long:
						message.Subject = $"[Alert] {schedule.Name}:{schedule.MonitorAsync.ServiceName} RECOVERED";
						message.Message = $"Incident#: #INCIDENTNUMBER#[NL]DateTime: {DateTime.UtcNow}[NL]Server Group: {schedule.Name}[NL] Health: {schedule.HealthStatus * 100.0:n2}%[NL]Flapping: {(schedule.IsFlapping ? "Yes" : "No")}[NL]{additionalData}Service: {schedule.MonitorAsync.ServiceName} has RECOVERED.[NL][NL]The service was down for {downLength}";
						break;
					case MessageFormat.Short:
						// max 160 chars
						message.Subject = $"{schedule.MonitorAsync.ServiceName} RECOVERED";
						message.Message = $"{additionalData}[NL]{schedule.MonitorAsync.ServiceName} RECOVERED.[NL][NL]Down for {downLength}";
						if(message.Message.Length > 160)
							message.Message = message.Message.Substring(0, 160);
						break;
				}

			}
			else
			{
				var downLength = "0 seconds";
				var lastUp = schedule.ResponseHistory.Where(x => x.IsUp).OrderByDescending(x => x.DateChecked).Select(x => x.DateChecked).FirstOrDefault();
				if (lastUp != null)
				{
					if (lastUp == DateTime.MinValue)
					{
						// there are no stats where this item has the required state (i.e. its always been down since app start)
						lastUp = System.Diagnostics.Process.GetCurrentProcess().StartTime;
					}
					var downTime = DateTime.UtcNow - lastUp;
					downLength = GetTimeMessage(downTime, messageFormat);
				}
				switch (messageFormat)
				{
					case MessageFormat.Long:
						message.Subject = $"[Alert] {schedule.Name}:{schedule.MonitorAsync.ServiceName} DOWN";
						message.Message = $"Incident#: #INCIDENTNUMBER#[NL]DateTime: {DateTime.UtcNow}[NL]Server Group: {schedule.Name}[NL]Health: {schedule.HealthStatus * 100.0:n2}%[NL]Flapping: {(schedule.IsFlapping ? "Yes" : "No")}[NL]{additionalData}Service: {schedule.MonitorAsync.ServiceName} is DOWN.[NL][NL]The service has been down for {downLength}";
						break;
					case MessageFormat.Short:
						// max 160 chars
						message.Subject = $"{schedule.MonitorAsync.ServiceName} DOWN";
						message.Message = $"{additionalData}[NL]{schedule.MonitorAsync.ServiceName} DOWN.[NL][NL]Down for {downLength}";
						if (message.Message.Length > 160)
							message.Message = message.Message.Substring(0, 160);
						break;
				}
			}

			// process custom tags
			foreach (KeyValuePair<string, string> kvp in customTags)
			{
				message.Subject = message.Subject.Replace($"#{kvp.Key}#", kvp.Value);
				message.Message = message.Message.Replace($"#{kvp.Key}#", kvp.Value);
			}

			return message;
		}

		private string GetTimeMessage(TimeSpan downTime, MessageFormat messageFormat)
		{
			var downLength = "0 seconds";
			if (downTime.TotalSeconds > 60 * 60 * 24)
			{
				if(messageFormat == MessageFormat.Long)
					downLength = $"{downTime.Days:n0} days {downTime.Hours:n0} hours {downTime.Minutes:n0} minutes";
				else
					downLength = $"{downTime.Days:n0} d {downTime.Hours:n0} hr {downTime.Minutes:n0} min";
			}
			else if (downTime.TotalSeconds > 60 * 60)
			{
				if (messageFormat == MessageFormat.Long)
					downLength = $"{downTime.Hours:n0} hours {downTime.Minutes:n0} minutes";
				else
					downLength = $"{downTime.Hours:n0} hr {downTime.Minutes:n0} min";
			}
			else if (downTime.TotalSeconds > 60)
			{
				if (messageFormat == MessageFormat.Long)
					downLength = $"{downTime.TotalMinutes:n0} minutes";
				else
					downLength = $"{downTime.TotalMinutes:n0} min";
			}
			else
			{
				if (messageFormat == MessageFormat.Long)
					downLength = $"{downTime.TotalSeconds:n0} seconds";
				else
					downLength = $"{downTime.TotalSeconds:n0} sec";
			}
			return downLength;
		}

		private string ParseTemplateKeywords(string message, bool isHtml)
		{
			message = message.Replace("\r\n", "[NL]");
			if (isHtml)
			{
				message = message.Replace("[NL]", Environment.NewLine + "<br/>");
			}
			else
			{
				message = message.Replace("[NL]", Environment.NewLine);
			}
			return message;
		}
	}
}