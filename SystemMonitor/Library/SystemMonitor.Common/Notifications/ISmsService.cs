namespace SystemMonitor.Common.Notifications
{
    public interface ISmsService : INotificationRecipientService
    {
        Task SendAsync(string message, string toPhoneNumber);
    }
}