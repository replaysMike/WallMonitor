using System.Diagnostics;
using SystemMonitor.Common.IO;
using SystemMonitor.Common.Models;

namespace SystemMonitor.Common.Notifications
{
    public class NotificationTracker
    {
        private readonly object _lock = new ();
        private readonly Dictionary<NotificationTypes, List<NotificationEntry>> _notificationTracker = new ()
        {
            { NotificationTypes.Email, new List<NotificationEntry>() },
            { NotificationTypes.Phone, new List<NotificationEntry>() },
        };

        public bool CanNotify(TimeSpan minAlertInterval, NotificationTypes notificationType, EscalationRecipient recipient, string hostName, string serviceName, ServiceState serviceState)
        {
            try
            {
                lock (_lock)
                {
                    var recipients = _notificationTracker[notificationType];
                    var index = recipients.FindIndex(x => x.Recipient == recipient && x.HostName == hostName && x.ServiceName == serviceName && x.ServiceState == serviceState);
                    if (index == -1)
                        return true;
                    var entry = recipients[index];
                    var now = DateTime.UtcNow;
                    var elapsed = now - entry.LastNotification;
                    if (elapsed > minAlertInterval)
                    {
                        Debug.WriteLine($"[{notificationType}] Allowing notification to {recipient.Recipient}");
                        return true;
                    }

                    Debug.WriteLine($"[{notificationType}] Preventing notification to {recipient.Recipient} due to minAlertLevel {minAlertInterval}");
                    return false;
                }
            }
            finally
            {
                RemoveOldEntries();
            }
        }

        public void DidNotify(NotificationTypes notificationType, EscalationRecipient recipient, string hostName, string serviceName, ServiceState serviceState)
        {
            lock (_lock)
            {
                var recipients = _notificationTracker[notificationType];
                var index = recipients.FindIndex(x => x.Recipient == recipient && x.HostName == hostName && x.ServiceName == serviceName && x.ServiceState == serviceState);

                if (index >= 0)
                {
                    var entry = recipients[index];
                    entry.LastNotification = DateTime.UtcNow;
                }
                else
                {
                    var now = DateTime.UtcNow;
                    var entry = new NotificationEntry(recipient, hostName, serviceName, serviceState, now, now);
                    recipients.Add(entry);
                }
            }
        }

        private void RemoveOldEntries()
        {
            lock (_lock)
            {
                foreach (var notificationTracker in _notificationTracker)
                {
                    notificationTracker.Value.RemoveAll(x => DateTime.UtcNow - x.LastNotification > TimeSpan.FromHours(48));
                }
            }
        }
    }

    public class NotificationEntry : IEqualityComparer<NotificationEntry>
    {
        public EscalationRecipient Recipient { get; init; }
        public string HostName { get; init; }
        public string ServiceName { get; init; }
        public ServiceState ServiceState { get; init; }
        public DateTime FirstNotification { get; set; }
        public DateTime LastNotification { get; set; }

        public NotificationEntry(EscalationRecipient recipient, string hostName, string serviceName, ServiceState serviceState)
        {
            Recipient = recipient;
            HostName = hostName;
            ServiceName = serviceName;
            ServiceState = serviceState;
        }

        public NotificationEntry(EscalationRecipient recipient, string hostName, string serviceName, ServiceState serviceState, DateTime firstNotification, DateTime lastNotification)
        {
            Recipient = recipient;
            HostName = hostName;
            ServiceName = serviceName;
            ServiceState = serviceState;
            FirstNotification = firstNotification;
            LastNotification = lastNotification;
        }

        public bool Equals(NotificationEntry x, NotificationEntry y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Recipient.Equals(y.Recipient) && x.HostName == y.HostName && x.ServiceName == y.ServiceName && x.ServiceState == y.ServiceState;
        }

        public int GetHashCode(NotificationEntry obj)
        {
            return HashCode.Combine(obj.Recipient, obj.HostName, obj.ServiceName, (int)obj.ServiceState);
        }
    }
}
