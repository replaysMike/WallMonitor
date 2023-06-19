namespace SystemMonitor.Common.Notifications
{
    public class EscalationRecipient : IEqualityComparer<EscalationRecipient>
    {
        /// <summary>
        /// The priority
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// The recipient
        /// </summary>
        public string Recipient { get; set; } = string.Empty;

        public bool Equals(EscalationRecipient? x, EscalationRecipient? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Recipient == y.Recipient;
        }

        public int GetHashCode(EscalationRecipient obj)
        {
            return HashCode.Combine(obj.Recipient);
        }
    }
}
