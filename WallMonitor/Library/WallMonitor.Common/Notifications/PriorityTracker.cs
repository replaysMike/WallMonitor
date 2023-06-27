using System.Diagnostics;
using WallMonitor.Common.IO;
using WallMonitor.Common.Models;

namespace WallMonitor.Common.Notifications
{
    public class PriorityTracker
    {
        private readonly Dictionary<NotificationTypes, List<EscalationRecipient>> _recipients = new();
        private readonly Dictionary<NotificationTypes, RecipientGroup> _currentGroup = new();

        public IEnumerable<EscalationRecipient> GetRecipientGroup(TimeSpan escalationTime, NotificationTypes notificationType, ServiceState serviceState, out bool isEscalated)
        {
            isEscalated = false;
            RecipientGroup? currentGroup = null;
            if (_currentGroup.ContainsKey(notificationType))
                currentGroup = _currentGroup[notificationType];

            // check if we should reset the escalation group back to the beginning
            var shouldReset = serviceState == ServiceState.Up;
            if (shouldReset)
            {
                if (currentGroup != null)
                {
                    // alert all notification groups up to the current about the UP state
                    var multipleGroups = _recipients[notificationType]
                        .Where(x => x.Priority <= currentGroup.Priority)
                        .ToList();
                    Reset(notificationType);
                    return multipleGroups;
                }
            }

            if (!_currentGroup.ContainsKey(notificationType))
            {
                // get the lowest group number
                var group = _recipients[notificationType].GroupBy(x => x.Priority).OrderBy(x => x.Key).FirstOrDefault();
                _currentGroup.Add(notificationType, new RecipientGroup(group.Key));
            }

            // check if the existing group needs to be escalated (only on a down notification)
            currentGroup = _currentGroup[notificationType];

            var shouldEscalate = (serviceState == ServiceState.Down || serviceState == ServiceState.Error) 
                                 && DateTime.UtcNow - currentGroup.FirstMessageTime >= escalationTime;
            if (shouldEscalate)
            {
                // escalate to the next group
                isEscalated = true;
                var group = _recipients[notificationType].GroupBy(x => x.Priority).Where(x => x.Key > currentGroup.Priority).OrderBy(x => x.Key).FirstOrDefault();
                if (group == null)
                    group = _recipients[notificationType].GroupBy(x => x.Priority).OrderBy(x => x.Key).FirstOrDefault();
                currentGroup = new RecipientGroup(group.Key);
                _currentGroup[notificationType] = currentGroup;
                Debug.WriteLine($"[{notificationType}] Escalated group to priority {group.Key}");
            }
            Debug.WriteLine($"[{notificationType}] Notifying priority group {currentGroup.Priority}");
            return _recipients[notificationType].Where(x => x.Priority == currentGroup.Priority).ToList();
        }

        public void SetRecipients(NotificationTypes notificationType, IEnumerable<EscalationRecipient> recipients)
        {
            _recipients.Add(notificationType, recipients.ToList());
        }

        public void Reset(NotificationTypes notificationType)
        {
            if (_currentGroup.ContainsKey(notificationType))
            {
                _currentGroup.Remove(notificationType);
                Debug.WriteLine($"De-escalated group to lowest priority.");
            }
        }
    }

    public class RecipientGroup
    {
        public int Priority { get; init; }
        public DateTime FirstMessageTime { get; set; }

        public RecipientGroup(int priority)
        {
            Priority = priority;
            FirstMessageTime = DateTime.UtcNow;
        }
    }
}
