namespace WallMonitor.Common.Notifications
{
    public interface INotificationConfiguration
    {
        /// <summary>
        /// True to enable notification service
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// The list of recipients
        /// </summary>
        IEnumerable<EscalationRecipient> Recipients { get; set; }
        
        /// <summary>
        /// The interval to escalate to the next recipient. Default: 30 minutes
        /// </summary>
        TimeSpan EscalationInterval { get; set; }

        /// <summary>
        /// The minimum interval that recipients will be notified. Default: 5 minutes
        /// </summary>
        TimeSpan MinAlertInterval { get; set; }
    }
}
