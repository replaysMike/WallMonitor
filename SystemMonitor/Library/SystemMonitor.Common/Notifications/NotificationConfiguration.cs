namespace SystemMonitor.Common.Notifications
{
    public class NotificationConfiguration
    {
        public EmailConfiguration Email { get; set; } = new();
        public SmsConfiguration Sms { get; set; } = new();
        public SnmpConfiguration Snmp { get; set; } = new();
    }
}
