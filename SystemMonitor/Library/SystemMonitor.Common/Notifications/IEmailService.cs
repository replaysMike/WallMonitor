using FluentEmail.Core.Interfaces;

namespace SystemMonitor.Common.Notifications
{
    public interface IEmailService : ISender, INotificationRecipientService
    {
        /// <summary>
        /// Get the email configuration
        /// </summary>
        EmailConfiguration EmailConfiguration { get; }

        /// <summary>
        /// Send an email
        /// </summary>
        /// <param name="emailContext"></param>
        Task SendEmailAsync(EmailContext emailContext);

        /// <summary>
        /// Send multiple emails
        /// </summary>
        /// <param name="emailContexts"></param>
        Task SendEmailsAsync(List<EmailContext> emailContexts);

        /// <summary>
        /// Send an email using a template
        /// </summary>
        /// <param name="emailContext"></param>
        /// <returns></returns>
        Task SendEmailTemplateAsync(EmailContext emailContext);
    }
}
