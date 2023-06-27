using WallMonitor.Common.Abstract;
using WallMonitor.Common.IO;

namespace WallMonitor.Common.Notifications
{
    public interface INotificationRecipientService
    {
        /// <summary>
        /// Send a notification message
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="eventType"></param>
        /// <param name="serviceState"></param>
        /// <param name="schedule"></param>
        /// <param name="isEscalated"></param>
        /// <returns></returns>
        Task<bool> SendMessageAsync(string recipient, EventType eventType, ServiceState serviceState, ISchedule schedule, bool isEscalated);
    }
}
