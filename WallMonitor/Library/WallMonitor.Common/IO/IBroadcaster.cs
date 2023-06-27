using WallMonitor.Common.IO.Messages;

namespace WallMonitor.Common.IO
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
