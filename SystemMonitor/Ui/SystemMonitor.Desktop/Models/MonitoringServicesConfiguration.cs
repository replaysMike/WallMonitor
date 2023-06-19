using System;
using System.Collections.Generic;

namespace SystemMonitor.Desktop.Models
{
    public class MonitoringServicesConfiguration
    {
        public List<MonitorServiceHost> MonitorHosts { get; set; } = new();
        public OrderBy OrderBy { get; set; } = OrderBy.DefinedByService;
        public OrderDirection OrderDirection { get; set; } = OrderDirection.DefinedByService;
        public UiSize Size { get; set; }

        /// <summary>
        /// True to enable audio alerts
        /// </summary>
        public bool AudioAlerts { get; set; } = true;

        /// <summary>
        /// True to enable progressive audio that gets louder/more obnoxious when more alerts are triggered
        /// </summary>
        public bool ProgressiveAudio { get; set; } = true;

        /// <summary>
        /// Set the progressive maximum audio alert level
        /// </summary>
        public AudioAlertLevel AudioAlertLevel { get; set; } = AudioAlertLevel.Normal;

        /// <summary>
        /// True to cycle between pages
        /// </summary>
        public bool CyclePages { get; set; } = true;

        /// <summary>
        /// The interval to cycle pages
        /// </summary>
        public TimeSpan CycleInterval { get; set; } = TimeSpan.FromSeconds(10);
    }

    public enum UiSize
    {
        Small,
        Normal,
        Large,
        Huge
    }

    public enum AudioAlertLevel
    {
        Quiet,
        Normal,
        Obnoxious
    }

    public enum OrderBy
    {
        DefinedByService = 0,
        DisplayName,
        Hostname
    }

    public enum OrderDirection
    {
        DefinedByService = 0,
        Ascending,
        Descending
    }

    public class MonitorServiceHost
    {
        /// <summary>
        /// Name of monitoring service instance
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Udp multicast address to listen to
        /// </summary>
        public string Endpoint { get; set; } = null!;

        /// <summary>
        /// Defines the display order id
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// An encryption key if you wish to encrypt the monitoring data
        /// </summary>
        public string? EncryptionKey { get; set; }
    }
}
