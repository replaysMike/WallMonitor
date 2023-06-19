using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;
using SystemMonitor.Common.Abstract;
using SystemMonitor.Common.IO;

namespace SystemMonitor.Common.Notifications
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfiguration _configuration;
        private readonly FluentEmailAwsSesRawSender _fluentAwsSesSender;
        private readonly IFluentEmailFactory _fluentEmailFactory;
        private readonly EmailTemplateManager _emailTemplateManager;
        private readonly ILogger<EmailService> _logger;

        public EmailConfiguration EmailConfiguration => _configuration;

        public EmailService(ILogger<EmailService> logger, FluentEmailAwsSesRawSender fluentSender, IFluentEmailFactory fluentEmailFactory, EmailConfiguration configuration, EmailTemplateManager emailTemplateManager)
        {
            _logger = logger;
            _fluentAwsSesSender = fluentSender;
            _fluentEmailFactory = fluentEmailFactory;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _emailTemplateManager = emailTemplateManager;

            if (_configuration.Enabled)
            {
                if (string.IsNullOrEmpty(_configuration.FromEmailAddress))
                    throw new ArgumentNullException(nameof(_configuration.FromEmailAddress));
                if (string.IsNullOrEmpty(_configuration.FromEmailName))
                    throw new ArgumentNullException(nameof(_configuration.FromEmailName));
            }
        }

        /// <summary>
        /// Send an email
        /// </summary>
        /// <param name="emailContext"></param>
        public async Task SendEmailAsync(EmailContext emailContext)
        {
            await SendEmailToContextUsingFluentAsync(emailContext);
        }

        /// <summary>
        /// Send multiple emails
        /// </summary>
        /// <param name="emailContexts"></param>
        public async Task SendEmailsAsync(List<EmailContext> emailContexts)
        {
            foreach (var emailContext in emailContexts)
                await SendEmailToContextUsingFluentAsync(emailContext);
        }

        /// <summary>
        /// Send an email using a template
        /// </summary>
        /// <param name="emailContext"></param>
        /// <returns></returns>
        public async Task SendEmailTemplateAsync(EmailContext emailContext)
        {
            await SendEmailToContextUsingFluentTemplateAsync(emailContext);
        }

        public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
        {
            // send mail using Ses
            return _fluentAwsSesSender.Send(email, token);
        }

        public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
        {
            // send mail using Ses
            return await _fluentAwsSesSender.SendAsync(email, token);
        }

        private async Task SendEmailToContextUsingFluentAsync(EmailContext emailContext)
        {
            if (!_configuration.Enabled) return;

            try
            {
                var emailTo = emailContext.Email;
                var email = _fluentEmailFactory
                    .Create()
                    .ReplyTo(_configuration.ReplyToEmailAddress)
                    .SetFrom(_configuration.FromEmailAddress, _configuration.FromEmailName)
                    .To(emailTo)
                    .Subject(emailContext.Subject)
                    .Body(emailContext.Body, true);
                _logger.LogInformation($"Sending email to '{emailTo}'");
                var response = await email.SendAsync();
                if (response.Successful)
                {
                    _logger.LogInformation($"Email sent to '{emailTo}'");
                }
                else
                {
                    _logger.LogError($"Failed to send email to '{emailTo}'! {string.Join(",", response.ErrorMessages)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email due to exception!");
            }
        }

        private async Task SendEmailToContextUsingFluentTemplateAsync(EmailContext emailContext)
        {
            if (!_configuration.Enabled) return;

            try
            {
                var emailTo = emailContext.Email;
                var renderedTemplate = await _emailTemplateManager.GetTemplateAsync(emailContext.TemplateName, emailContext.TemplateData);
                var email = _fluentEmailFactory
                    .Create()
                    .ReplyTo(_configuration.ReplyToEmailAddress)
                    .SetFrom(_configuration.FromEmailAddress, _configuration.FromEmailName)
                    .To(emailTo)
                    .Subject(emailContext.Subject)
                    .Body(renderedTemplate, true);

                _logger.LogInformation($"Sending email to '{emailTo}'");
                var response = await email.SendAsync();
                if (response.Successful)
                {
                    _logger.LogInformation($"Email sent to '{emailTo}'");
                }
                else
                {
                    _logger.LogError($"Failed to send email to '{emailTo}'! {string.Join(",", response.ErrorMessages)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email due to exception!");
            }
        }

        public async Task<bool> SendMessageAsync(string recipient, EventType eventType, ServiceState serviceState, ISchedule schedule, bool isEscalated)
        {
            if (!_configuration.Enabled) return false;

            var message = $"{(isEscalated ? "[ESCALATED] " : "")}Service {schedule.Name} on {schedule.Host?.Name} is <b>{serviceState.ToString().ToUpper()}</b>";
            message += $"<br/><br/>\r\n\r\n";
            if (serviceState != ServiceState.Up)
            {
                message += $"Attempt #: <b>{schedule.ConsecutiveAttempts}</b><br/>\r\n";
                message += $"Service down for: <b>{Util.GetFriendlyElapsedTime(DateTime.UtcNow.Subtract(schedule.LastUpTime))}</b><br/>\r\n";
            }
            else
            {
                message += $"Service was down for: {Util.GetFriendlyElapsedTime(schedule.PreviousDownTime)}<br/>\r\n";
            }

            var context = new EmailContext
            {
                Name = recipient,
                Email = recipient,
                Body = message,
                Subject = $"{(isEscalated ? "[ESCALATED] " : "")}{schedule.Name} on {schedule.Host?.Name} is {serviceState.ToString().ToUpper()}",
            };

            await SendEmailAsync(context);

            return true;
        }
    }
}
