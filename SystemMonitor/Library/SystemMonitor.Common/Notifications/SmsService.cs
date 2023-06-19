using Microsoft.Extensions.Logging;
using SystemMonitor.Common.Abstract;
using SystemMonitor.Common.IO;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace SystemMonitor.Common.Notifications
{
    public class SmsService : ISmsService
    {
        private SmsConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;

        public SmsService(ILogger<SmsService> logger, SmsConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (_configuration.Enabled)
            {
                if (string.IsNullOrEmpty(_configuration.AccountSid))
                    throw new ArgumentNullException(nameof(_configuration.AccountSid));
                if (string.IsNullOrEmpty(_configuration.AuthToken))
                    throw new ArgumentNullException(nameof(_configuration.AuthToken));
                if (string.IsNullOrEmpty(_configuration.FromPhoneNumber))
                    throw new ArgumentNullException(nameof(_configuration.FromPhoneNumber));
            }
        }

        public Task SendAsync(string message, string toPhoneNumber)
        {
            if (!_configuration.Enabled) return Task.CompletedTask;

            TwilioClient.Init(_configuration.AccountSid, _configuration.AuthToken);

            var messageResponse = MessageResource.Create(
                body: message,
                from: new Twilio.Types.PhoneNumber(_configuration.FromPhoneNumber),
                to: new Twilio.Types.PhoneNumber(toPhoneNumber)
            );
            if (messageResponse.Status != MessageResource.StatusEnum.Failed)
                _logger.LogInformation($"SMS sent to '{toPhoneNumber}'");
            else
                _logger.LogError($"Failed to send SMS to '{toPhoneNumber}'. Error Status: {messageResponse.Status} Error Message: '{messageResponse.ErrorMessage}' Error Code: {messageResponse.ErrorCode}");
            return Task.CompletedTask;
        }

        public async Task<bool> SendMessageAsync(string recipient, EventType eventType, ServiceState serviceState, ISchedule schedule, bool isEscalated)
        {
            if (!_configuration.Enabled) return false;

            var message = $"Service '{schedule.Name}' on server '{schedule.Host?.Name}'  is {serviceState.ToString().ToUpper()}";
            message += $"\r\n\r\n";
            if (serviceState != ServiceState.Up)
            {
                message += $"Attempt #: {schedule.ConsecutiveAttempts}\r\n";
                message += $"Service down for: {Util.GetFriendlyElapsedTime(DateTime.UtcNow.Subtract(schedule.LastUpTime))}\r\n";
            }
            else
            {
                message += $"Service was down for: {Util.GetFriendlyElapsedTime(schedule.PreviousDownTime)}\r\n";
            }

            await SendAsync(message, recipient);

            return true;
        }
    }
}
