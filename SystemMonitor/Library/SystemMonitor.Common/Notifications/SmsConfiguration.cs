namespace SystemMonitor.Common.Notifications
{
    public class SmsConfiguration : INotificationConfiguration
    {
        /// <inheritdoc/>
        public bool Enabled { get; set; }

        /// <inheritdoc/>
        public IEnumerable<EscalationRecipient> Recipients { get; set; } = new List<EscalationRecipient>();

        /// <inheritdoc/>
        public TimeSpan EscalationInterval { get; set; } = TimeSpan.FromMinutes(30);

        /// <inheritdoc/>
        public TimeSpan MinAlertInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Account Id
        /// </summary>
        public string? AccountSid { get; set; }

        /// <summary>
        /// Secret auth token for api
        /// </summary>
        public string? AuthToken { get; set; }

        /// <summary>
        /// The phone number to use for reply messages.
        /// This number must be registered on Twilio and costs per month.
        /// </summary>
        public string? FromPhoneNumber { get; set; }
    }
}
