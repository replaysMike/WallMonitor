using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using SystemMonitor.Resources;

namespace SystemMonitor.Desktop.Controls
{
    public class StatusCountButton : Button
    {
        private readonly IImage _whiteImage;
        private readonly IImage _redImage;
        private const double AspectRatio = 1;
        private readonly DispatcherTimer _timer;
        private readonly TimeSpan _timerDelayInterval = TimeSpan.FromSeconds(3);

        public static readonly DirectProperty<StatusCountButton, bool> IsMutedProperty =
            AvaloniaProperty.RegisterDirect<StatusCountButton, bool>(nameof(IsRed), o => o.IsRed, (o, value) => o.IsRed = value);
        public static readonly DirectProperty<StatusCountButton, int> CountProperty =
            AvaloniaProperty.RegisterDirect<StatusCountButton, int>(nameof(Count), o => o.Count, (o, value) => o.Count = value);

        private bool _isRed = false;
        public bool IsRed
        {
            get => _isRed;
            set
            {
                SetAndRaise(IsMutedProperty, ref _isRed, value);
                InvalidateVisual();
            }
        }

        private int _count = 0;
        public int Count
        {
            get => _count;
            set
            {
                SetAndRaise(CountProperty, ref _count, value);
                _isRed = _count > 0;
                if (_isRed) 
                    _timer.Interval = TimeSpan.Zero;
                else
                    _timer.Interval = _timerDelayInterval;
                _timer.Start();
                InvalidateVisual();
            }
        }

        public StatusCountButton()
        {
            IsHitTestVisible = true;
            PressedMixin.Attach<AudioMuteButton>();
            _whiteImage = new Bitmap(ResourceLoader.LoadStream($"button_white.png", ResourceType.Sprite, ImageResourceResolution.HD4K));
            _redImage = new Bitmap(ResourceLoader.LoadStream($"button_red.png", ResourceType.Sprite, ImageResourceResolution.HD4K));
            _timer = new DispatcherTimer
            {
                Interval = _timerDelayInterval
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            if (Count == 0)
                Opacity = 0.25;
            else
                Opacity = 1;
        }

        public override void Render(DrawingContext context)
        {
            if (_isRed)
            {
                context.DrawImage(_redImage, new Avalonia.Rect(0, 0, Width, double.IsNaN(Height) ? Width * AspectRatio : Height));
            }
            else
            {
                context.DrawImage(_whiteImage, new Avalonia.Rect(0, 0, Width, double.IsNaN(Height) ? Width * AspectRatio : Height));
            }

            var text = new FormattedText($"{Count}", System.Threading.Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 32, Foreground);
            context.DrawText(text, new Point(Width / 2 - text.Width / 2, Height / 2 - text.Height / 2));
        }
    }
}
