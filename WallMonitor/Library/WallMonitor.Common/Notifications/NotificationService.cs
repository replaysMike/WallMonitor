using Microsoft.Extensions.Logging;
using WallMonitor.Common.Abstract;
using WallMonitor.Common.IO;
using WallMonitor.Common.Models;

namespace WallMonitor.Common.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ISnmpService _snmpService;
        private readonly NotificationConfiguration _notificationConfiguration;
        private readonly NotificationTracker _notificationTracker = new ();
        private readonly PriorityTracker _priorityTracker = new ();

        public NotificationService(ILogger<NotificationService> logger, NotificationConfiguration notificationConfiguration, IEmailService emailService, ISmsService smsService, ISnmpService snmpService)
        {
            _logger = logger;
            _notificationConfiguration = notificationConfiguration;
            _emailService = emailService;
            _smsService = smsService;
            _snmpService = snmpService;
            _priorityTracker.SetRecipients(NotificationTypes.Email, _notificationConfiguration.Email.Recipients);
            _priorityTracker.SetRecipients(NotificationTypes.Phone, _notificationConfiguration.Sms.Recipients);
        }

        public async Task<bool> SendMessageAsync(EventType eventType, ServiceState serviceState, ISchedule schedule)
        {
            if (_notificationConfiguration.Email.Enabled)
            {
                foreach (var recipient in _priorityTracker.GetRecipientGroup(_notificationConfiguration.Email.EscalationInterval, NotificationTypes.Email, serviceState, out var isEscalated))
                {
                    try
                    {
                        if (_notificationTracker.CanNotify(_notificationConfiguration.Email.MinAlertInterval, NotificationTypes.Email, recipient, schedule.Host.Name, schedule.Name, serviceState))
                        {
                            await _emailService.SendMessageAsync(recipient.Recipient, eventType, serviceState, schedule, isEscalated);
                            _notificationTracker.DidNotify(NotificationTypes.Email, recipient, schedule.Host.Name, schedule.Name, serviceState);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send email to recipient '{recipient.Recipient}'");
                    }
                }
            }

            if (_notificationConfiguration.Sms.Enabled)
            {
                foreach (var recipient in _priorityTracker.GetRecipientGroup(_notificationConfiguration.Sms.EscalationInterval, NotificationTypes.Phone, serviceState, out var isEscalated))
                {
                    try
                    {
                        if (_notificationTracker.CanNotify(_notificationConfiguration.Sms.MinAlertInterval, NotificationTypes.Phone, recipient, schedule.Host.Name, schedule.Name, serviceState))
                        {
                            await _smsService.SendMessageAsync(recipient.Recipient, eventType, serviceState, schedule, isEscalated);
                            _notificationTracker.DidNotify(NotificationTypes.Phone, recipient, schedule.Host.Name, schedule.Name, serviceState);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send sms to recipient '{recipient.Recipient}'");
                    }
                }
            }

            if (_notificationConfiguration.Snmp.Enabled)
            {
                await _snmpService.SendMessageAsync(string.Empty, eventType, serviceState, schedule, false);
            }

            return true;
        }
    }
}
