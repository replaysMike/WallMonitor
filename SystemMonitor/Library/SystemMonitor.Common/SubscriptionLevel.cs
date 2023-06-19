namespace SystemMonitor.Common
{
    public enum SubscriptionLevel
    {
        /// <summary>
        /// No license allows up to 6 servers, 30 services
        /// </summary>
        None,
        /// <summary>
        /// Personal restricted to 15 servers, 90 services
        /// </summary>
        Personal,
        /// <summary>
        /// Professional restricted to 50 servers, 300 services
        /// </summary>
        Professional,
        /// <summary>
        /// Enterprise subscriptions are unlimited servers/services
        /// </summary>
        Enterprise
    }
}
