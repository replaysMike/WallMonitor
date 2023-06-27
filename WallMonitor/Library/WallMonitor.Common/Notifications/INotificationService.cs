using WallMonitor.Common.Abstract;
using WallMonitor.Common.IO;

namespace WallMonitor.Common.Notifications
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
