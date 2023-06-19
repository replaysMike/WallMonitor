namespace SystemMonitor.Common.Notifications
{
    public class EmailConfiguration : INotificationConfiguration
    {
        /// <inheritdoc/>
        public bool Enabled { get; set; }

        /// <inheritdoc/>
        public IEnumerable<EscalationRecipient> Recipients { get; set; } = new List<EscalationRecipient>();

        /// <inheritdoc/>
        public TimeSpan EscalationInterval { get; set; } = TimeSpan.FromMinutes(30);

        /// <inheritdoc/>
        public TimeSpan MinAlertInterval { get; set; } = TimeSpan.FromMinutes(5);

        public EmailProviders EmailProvider { get; set; } = EmailProviders.Smtp;

        /// <summary>
        /// Smtp server address
        /// </summary>
        public string SmtpServer { get; set; } = "127.0.0.1";

        /// <summary>
        /// Smtp port number
        /// </summary>
        public int Port { get; set; } = 25;

        /// <summary>
        /// Smtp login credential username (optional)
        /// </summary>
        public string? SmtpUsername { get; set; }

        /// <summary>
        /// Smtp login credential password (optional)
        /// </summary>
        public string? SmtpPassword { get; set; }

        /// <summary>
        /// Email send timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The email address emails will be sent from
        /// </summary>
        public string ReplyToEmailAddress { get; set; } = "no-reply@systemmonitor.net";

        /// <summary>
        /// The email address emails will be sent from
        /// </summary>
        public string FromEmailAddress { get; set; } = "no-reply@systemmonitor.net";

        /// <summary>
        /// The email address emails will be sent from
        /// </summary>
        public string FromEmailName { get; set; } = "SystemMonitor Alerts";

        /// <summary>
        /// AWS SES Email configuration
        /// </summary>
        public AwsSesEmailConfiguration AwsSesConfiguration { get; set; } = new();
    }
}
