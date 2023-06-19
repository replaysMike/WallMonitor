using SystemMonitor.Common.Abstract;
using SystemMonitor.Common.IO;

namespace SystemMonitor.Common.Notifications
{
    public interface INotificationService
    {
        /// <summary>
        /// Send a notification message
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="serviceState"></param>
        /// <param name="schedule"></param>
        /// <returns></returns>
        Task<bool> SendMessageAsync(EventType eventType, ServiceState serviceState, ISchedule schedule);
    }
}
