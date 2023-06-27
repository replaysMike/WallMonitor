using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WallMonitor.Desktop.ViewModels
{
    public class LoaderViewModel : INotifyPropertyChanged
    {
        private static readonly string[] Messages =
        {
            "Syncing...", 
            "Fueling booster rockets...", 
            "Targeting phasers...", 
            "Preparing for warp...", 
            "Flicking toggles...", 
            "Warming buttons...",
            "Fixing bugs...",
            "Randomizing cache values...",
            "Ordering pizza...",
            "Formatting memory...",
            "Calculating PI...",
            "Simulating life...",
            "Dividing by 0...",
            "Loading...",
            "Preflight check...",
            "Allocating 640kb...",
            "Generating witty dialog...",
            "Tokenizing atoms...",
            "Don't panic...",
            "Computing offsets...",
            "Windows is better in Unix...",
            "Cracking military encryption...",
            "Trying to sort in O(n)...",
            "Spawn more Overlords...",
            "Five-by-five...",
            "Loading funny message...",
            "Formatting C:... wait what?",
            "Rounding pixels...",
            "Placing breakpoints...",
            "Downloading downloader...",
            "Cleaning viruses...",
            "Removing clouds...",
            "Pressing turbo button...",
            "Detecting swag...",
            "Detecting duplex mode...",
            "Detecting keyboard color...",
            "Detecting CRT type...",
            "Unicoding ascii strings...",
            "Detecting mouse size...",
            "Cleaning named pipes...",
            "Detecting drive dimensions...",
            "Detecting cursor blink rate...",
            "Enlarging partitions...",
            "Removing unnecessary partitions...",
            "Lighting pilot light...",
            "Contacting mothership...",
            "Sorting dram...",
            "Warming cooling liquid...",
            "Optimizing network byte order...",
            "Aw, snap! j/k",
            "Engage Number one.",
            "Upgrading to Windows Vista...",
            "Removing semicolons...",
            "Encoding URLs...",
            "Uninstalling javascript...",
            "git pull github.com/*",
            "Initializing ChatGPT...",
            "Removing 0's from binary...",
            "XORing passwords...",
            "Disabling any key...",
            "Formatting json...",
            "Filtering filters...",
            "Reversing line endings...",
            "Raising your seat...",
            "Playing hold music..."
        };
        private readonly DispatcherTimer _timer;
        private readonly Random _random = new Random();

        private string _text = string.Empty;

        public string Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }

        public LoaderViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _timer.Tick += _timer_Tick;
            Text = Messages[_random.Next(0, Messages.Length)];
            _timer.Start();
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            Text = Messages[_random.Next(0, Messages.Length)];
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
