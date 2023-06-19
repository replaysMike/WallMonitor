using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using SystemMonitor.Common.Models;
using SystemMonitor.Desktop.Services;

namespace SystemMonitor.Desktop.Controls
{
    public class AnimatedBackgroundCanvas : Control
    {
        private const int Fps = 60;
        private const double PulsePerSecond = 3.0; // control the speed
        private readonly LinearGradientBrush _brush;
        private int _ticks;
        private int _direction = 1;
        private readonly TimeSpan _frameRate = TimeSpan.FromSeconds(1d / Fps);
        private readonly StateColor _stateColor = new ();
        public SystemAlertLevel AlertLevel { get; set; } = SystemAlertLevel.Info;
        private IMessageNotificationService? _messageNotificationService;

        public AnimatedBackgroundCanvas()
        {
            SetAlertLevel(AlertLevel);

            _brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(1, 0, RelativeUnit.Relative)
            };

            var timer = new DispatcherTimer { Interval = _frameRate };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        public void SetMessageNotificationService(IMessageNotificationService messageNotificationService)
        {
            _messageNotificationService = messageNotificationService;
            _messageNotificationService.OnReceiveMonitoringServiceMessage += OnReceiveMessage;
            _messageNotificationService.OnReceiveServerMessage += OnReceiveServerMessage;
        }

        private void OnReceiveServerMessage(object sender, ServerMessageEventArgs e)
        {
            switch (e.Message.EventType)
            {
                case Common.IO.EventType.ServiceOffline:
                    SetAlertLevel(SystemAlertLevel.Error);
                    break;
                case Common.IO.EventType.ServiceRestored:
                    SetAlertLevel(SystemAlertLevel.Info);
                    break;
            }
        }

        private void OnReceiveMessage(object sender, MonitoringServiceEventArgs e)
        {
            var message = e.Message;
            switch (message.EventType)
            {
                case Common.IO.EventType.HostCheckFailed:
                    SetAlertLevel(SystemAlertLevel.Error);
                    break;
                case Common.IO.EventType.HostCheckRecovered:
                    SetAlertLevel(SystemAlertLevel.Info);
                    break;
                case Common.IO.EventType.HostCheckStarted:
                    break;
                case Common.IO.EventType.HostCheckCompleted:
                    break;
            }
        }

        private void SetAlertLevel(SystemAlertLevel alertLevel)
        {
            switch (alertLevel)
            {
                case SystemAlertLevel.Success:
                    _stateColor.Red = 0;
                    _stateColor.Green = 255;
                    _stateColor.Blue = 0;
                    break;
                case SystemAlertLevel.Error:
                    _stateColor.Red = 255;
                    _stateColor.Green = 0;
                    _stateColor.Blue = 0;
                    break;
                case SystemAlertLevel.Info:
                    _stateColor.BaseAlpha = 0.9;
                    _stateColor.Red = 0;
                    _stateColor.Green = 80;
                    _stateColor.Blue = 180;
                    break;
                case SystemAlertLevel.Warning:
                    _stateColor.Red = 255;
                    _stateColor.Green = 255;
                    _stateColor.Blue = 0;
                    break;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_ticks <= 0) _direction = 1;
            if (_ticks >= Fps * (PulsePerSecond * 0.5)) _direction = -1;

            if (_direction > 0)
                _ticks++;
            else
                _ticks--;
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            // some help from: https://github.com/AvaloniaUI/Avalonia/blob/master/samples/RenderDemo/Controls/LineBoundsDemoControl.cs
            // based on: https://github.com/AvaloniaUI/Avalonia/issues/5947
            // cover window bounds
            const int min = 90;
            const int max = 200;
            var perc = _ticks / (Fps * 2.0);
            var easing = new QuadraticEaseOut();


            var opacity = (easing.Ease(perc) * (max - min)) + min;
            _brush.GradientStops = new GradientStops
            {
                // black to bottom
                new (Color.FromArgb(0,0,0,0), 0),
                new (Color.FromArgb(0,0,0,0), 0.75),
                // final color
                new (Color.FromArgb((byte)(opacity * _stateColor.BaseAlpha), _stateColor.Red, _stateColor.Green, _stateColor.Blue), 1),
            };
            context.DrawRectangle(_brush, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

        }

        public class StateColor
        {
            public double BaseAlpha { get; set; } = 1.0;
            public byte Red { get; set; }
            public byte Green { get; set; }
            public byte Blue { get; set; }
        }
    }
}
