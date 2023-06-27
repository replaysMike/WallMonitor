namespace WallMonitor.Common.Models
{
    /// <summary>
    /// A notification group member
    /// </summary>
    public class NotificationGroupMember
	{
		public string Name { get; set; }
		public string Email { get; set; }
		public string Phone { get; set; }
		public NotificationTypes NotificationMethods { get; set; }
		public bool Enabled { get; set; }
		public bool Html { get; set; }
		public MessageFormat MessageFormat { get; set; }

		public NotificationGroupMember(string name, string email, string phone)
		{
			Enabled = true;
			Name = name;
			Email = email;
			Phone = phone;
			if (!string.IsNullOrEmpty(Email))
				NotificationMethods = NotificationMethods | NotificationTypes.Email;
			if (!string.IsNullOrEmpty(Phone))
				NotificationMethods = NotificationMethods | NotificationTypes.Phone;
		}

		public NotificationGroupMember(string name, string email, string phone, bool enabled)
		{
			Enabled = enabled;
			Name = name;
			Email = email;
			Phone = phone;
			if (!string.IsNullOrEmpty(Email))
				NotificationMethods = NotificationMethods | NotificationTypes.Email;
			if (!string.IsNullOrEmpty(Phone))
				NotificationMethods = NotificationMethods | NotificationTypes.Phone;
		}

		public NotificationGroupMember(string name, string email, string phone, bool enabled, bool html, MessageFormat messageFormat)
		{
			Enabled = enabled;
			Name = name;
			Email = email;
			Phone = phone;
			Html = html;
			if (!string.IsNullOrEmpty(Email))
				NotificationMethods = NotificationMethods | NotificationTypes.Email;
			if (!string.IsNullOrEmpty(Phone))
				NotificationMethods = NotificationMethods | NotificationTypes.Phone;
			MessageFormat = messageFormat;
		}

		public NotificationGroupMember(string name, string email, string phone, NotificationTypes notificationMethods)
		{
			Enabled = true;
			Name = name;
			Email = email;
			Phone = phone;
			NotificationMethods = notificationMethods;
		}
		public NotificationGroupMember(string name, string email, string phone, NotificationTypes notificationMethods, bool enabled, bool html, MessageFormat messageFormat)
		{
			Enabled = enabled;
			Name = name;
			Email = email;
			Phone = phone;
			NotificationMethods = notificationMethods;
			Html = html;
			MessageFormat = messageFormat;
		}

	}
}
