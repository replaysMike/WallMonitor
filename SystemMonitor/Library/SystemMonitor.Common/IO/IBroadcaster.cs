using SystemMonitor.Common.IO.Messages;

namespace SystemMonitor.Common.IO
{
    public interface IBroadcaster : IDisposable
    {
        /// <summary>
        /// Send a server event message
        /// </summary>
        /// <param name="message"></param>
        Task SendAsync(ServerStatusUpdate message);
    }
}
