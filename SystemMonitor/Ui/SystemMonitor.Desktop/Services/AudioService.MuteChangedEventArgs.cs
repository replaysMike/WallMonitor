using System;

namespace SystemMonitor.Desktop.Services;

public partial class AudioService
{
    public class MuteChangedEventArgs : EventArgs
    {
        public bool OldValue { get; set; }
        public bool NewValue { get; set; }
        public MuteChangedEventArgs(bool oldValue, bool newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}