using WallMonitor.Common.IO;

namespace WallMonitor.Common
{
    public interface IListener : IDisposable
    {
        /// <summary>
        /// Server event update received
        /// </summary>
        event EventHandler<ServerNotificationEventArgs>? ServerEventReceived;

        /// <summary>
        /// Monitor service configuration
        /// </summary>
        event EventHandler<MonitorConfigurationEventArgs>? ConfigurationEventReceived;

        /// <summary>
        /// Connection lost event
        /// </summary>
        event EventHandler<ConnectionLostEventArgs>? ConnectionLost;

        /// <summary>
        /// Connection restored event
        /// </summary>
        event EventHandler<ConnectionRestoredEventArgs>? ConnectionRestored;

        /// <summary>
        /// Unique Id of the listener
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Name of the monitor service
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The order in which to display its monitored services in the results
        /// </summary>
        int OrderId { get; }

        /// <summary>
        /// Start receiving messages
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the server
        /// </summary>
        void Stop();
    }
}
