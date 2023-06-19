using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using SystemMonitor.Desktop.Models;
using SystemMonitor.Resources;

namespace SystemMonitor.Desktop.Controls
{
    public class ServerDownImage : Control
    {
        private readonly IImage _downImage;
        private readonly IImage _stateUnknownImage;
        private const double AspectRatio = 0.36;
        private const int Fps = 60;
        private const double PulsePerSecond = 3; // control the speed
        private int _ticks;
        private int _direction = 1;
        private bool _fadeOut = false;
        private DispatcherTimer _timer;
        private readonly TimeSpan _frameRate = TimeSpan.FromSeconds(1d / Fps);

        public static readonly DirectProperty<ServerDownImage, State> CurrentStateProperty =
            AvaloniaProperty.RegisterDirect<ServerDownImage, State>(nameof(CurrentState), o => o.CurrentState, (o, value) => o.CurrentState = value);

        private State _currentState = State.ServerIsNotRunning;
        public State CurrentState
        {
            get => _currentState;
            set
            {
                SetAndRaise(CurrentStateProperty, ref _currentState, value);
                if (value == State.ServerDown)
                {
                    _fadeOut= false;
                    _timer.Start();
                }
                else if (value == State.ServerIsRunning)
                {
                    _fadeOut = true;
                }
            }
        }

        public ServerDownImage()
        {
            IsHitTestVisible = false;
            _downImage = new Bitmap(ResourceLoader.LoadStream($"icon_down.png", ResourceType.Sprite, ImageResourceResolution.HD4K));
            _stateUnknownImage = new Bitmap(ResourceLoader.LoadStream($"icon_unknown.png", ResourceType.Sprite, ImageResourceResolution.HD4K));
            _timer = new DispatcherTimer { Interval = _frameRate };
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_fadeOut)
            {
                _ticks--;
                if (_ticks < 0)
                {
                    //Debug.WriteLine("Timer stopped!");
                    _timer.Stop();
                    _fadeOut= false;
                    Opacity = 0;
                    return;
                }
            }
            else
            {
                if (_ticks <= 0) _direction = 1;
                if (_ticks >= Fps * (PulsePerSecond * 0.5)) _direction = -1;

                if (_direction > 0)
                    _ticks++;
                else
                    _ticks--;
            }

            const int min = 150;
            const int max = 255;
            var perc = _ticks / (Fps * 2.0);
            var easing = new QuadraticEaseOut();
            var opacity = ((easing.Ease(perc) * (max - min)) + min) / 255d;
            Opacity = opacity;
        }

        public override void Render(DrawingContext context)
        {
            if (CurrentState == State.ServerDown)
            {
                context.DrawImage(_downImage, new Avalonia.Rect(0, 0, Width, Width * AspectRatio));
            }
            if (CurrentState == State.ParentMonitoringServiceIsDown)
            {
                context.DrawImage(_stateUnknownImage, new Avalonia.Rect(0, 0, Width, Width * AspectRatio));
            }
        }
    }
}
