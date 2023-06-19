using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SystemMonitor.Desktop.Views
{
    public partial class ToastContainer : UserControl
    {
        private ToastContainerContext ViewModel => (ToastContainerContext?)DataContext ?? new ToastContainerContext();
        private readonly ConcurrentQueue<ToastContext> _queue = new();

        public ToastContainer()
        {
            InitializeComponent();
            if (Design.IsDesignMode)
            {
                // add some toasts for design mode
                var toasts = new List<ToastContext>();
                toasts.Add(new ToastContext(ToastType.Info, "Default message to display to the user during a toast message that can be really long by spanning multiple lines"));
                toasts[0].Opacity = 1;
                toasts.Add(new ToastContext(ToastType.Success, "Server is up!"));
                toasts[1].Opacity = 1;
                DataContext = new ToastContainerContext(toasts);
            }
            else
            {
                DataContext = new ToastContainerContext();
            }

            // toasts get queued and displayed with a bit of a delay
            var queueTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            queueTimer.Tick += _queueTimer_Tick;
            queueTimer.Start();
        }

        private void _queueTimer_Tick(object? sender, EventArgs e)
        {
            if (_queue.Count > 0)
            {
                if (_queue.TryDequeue(out var newToast))
                {
                    // add the new toast and start its animations
                    ViewModel.Toasts.Add(newToast);
                    newToast.Open();
                }
            }
        }

        /// <summary>
        /// Show a toast
        /// </summary>
        /// <param name="toastType">The type of toast message</param>
        /// <param name="message">The message to display</param>
        public void Show(ToastType toastType, string message)
        {
            var newToast = new ToastContext(toastType, message)
            {
                OnClose = (toast) =>
                {
                    ViewModel.Toasts.Remove(toast);
                }
            };

            if (!Design.IsDesignMode)
            {
                _queue.Enqueue(newToast);
            }
            else
            {
                ViewModel.Toasts.Add(newToast);
            }
        }

        /// <summary>
        /// Show a toast
        /// </summary>
        /// <param name="toastType">The type of toast message</param>
        /// <param name="message">The message to display</param>
        /// <param name="duration">The duration to display the toast for</param>
        public void Show(ToastType toastType, string message, TimeSpan duration)
        {
            var newToast = new ToastContext(toastType, message, duration)
            {
                OnClose = (toast) =>
                {
                    ViewModel.Toasts.Remove(toast);
                }
            };

            if (!Design.IsDesignMode)
            {
                _queue.Enqueue(newToast);
            }
            else
            {
                ViewModel.Toasts.Add(newToast);
            }
        }

        /// <summary>
        /// Remove all toasts
        /// </summary>
        public void Clear()
        {
            foreach (var toast in ViewModel.Toasts)
            {
                toast.Close();
            }
        }
    }

    public class ToastContainerContext
    {
        public ObservableCollection<ToastContext> Toasts { get; set; } = new();
        public ToastContainerContext() { }

        public ToastContainerContext(ToastContext toast)
        {
            Toasts.Add(toast);
        }

        public ToastContainerContext(List<ToastContext> toasts)
        {
            Toasts.AddRange(toasts);
        }
    }

    public class ToastContext : INotifyPropertyChanged
    {
        private readonly TimeSpan _toastInterval = TimeSpan.FromMilliseconds(1000 / 70d);
        private readonly TimeSpan _defaultToastDuration = TimeSpan.FromMilliseconds(5000);

        private readonly DispatcherTimer _displayTimer;
        private readonly DispatcherTimer _showHideTimer;
        private int _direction = 1;

        public Action<ToastContext>? OnClose { get; set; }

        private string? _message;
        public string? Message
        {
            get => _message;
            set => SetField(ref _message, value);
        }

        private ToastType _toastType;
        public ToastType ToastType
        {
            get => _toastType;
            set
            {
                SetBackgroundColor(_toastType);
                SetField(ref _toastType, value);
            }
        }

        private double _scale;
        public double Scale
        {
            get => _scale;
            set => SetField(ref _scale, value);
        }

        private double _opacity;
        public double Opacity
        {
            get => _opacity;
            set => SetField(ref _opacity, value);
        }

        private string _background = "#0000aa";
        public string Background
        {
            get => _background;
            set => SetField(ref _background, value);
        }

        private string _border = "#0000dd";
        public string Border
        {
            get => _border;
            set => SetField(ref _border, value);
        }

        public ToastContext(ToastType toastType, string message, TimeSpan? toastDuration = null)
        {
            ToastType = toastType;
            Message = message;
            if (toastDuration == null) 
                toastDuration = _defaultToastDuration;

            if (Design.IsDesignMode)
            {
                Opacity = 1;
                Scale = 1;
            }
            else
            {
                Opacity = 0;
                Scale = 0;
            }

            SetBackgroundColor(toastType);

            _showHideTimer = new DispatcherTimer
            {
                Interval = _toastInterval
            };
            _showHideTimer.Tick += ShowHide_Tick;

            _displayTimer = new DispatcherTimer
            {
                Interval = toastDuration.Value
            };
            _displayTimer.Tick += Display_Tick;
        }

        public void Open()
        {
            if (!Design.IsDesignMode)
            {
                _showHideTimer.Start();
            }
        }

        private void SetBackgroundColor(ToastType toastType)
        {
            switch (toastType)
            {
                default:
                case ToastType.Info:
                    Background = "#0000aa";
                    Border = "#0000dd";
                    break;
                case ToastType.Success:
                    Background = "#00aa00";
                    Border = "#00dd00";
                    break;
                case ToastType.Warning:
                    Background = "#aaaa00";
                    Border = "#dddd00";
                    break;
                case ToastType.Error:
                    Background = "#aa0000";
                    Border = "#dd0000";
                    break;
            }
        }

        private void ShowHide_Tick(object? sender, EventArgs e)
        {
            // Debug.WriteLine($"ShowHide_Tick {Opacity}");
            if (_direction > 0)
            {
                Opacity += (1000d / _toastInterval.TotalMilliseconds / 1000);
                Scale += (1000d / _toastInterval.TotalMilliseconds / 1000);
            }
            else
            {
                Opacity -= (1000d / _toastInterval.TotalMilliseconds / 1000);
                Scale -= (1000d / _toastInterval.TotalMilliseconds / 1000);
            }

            if (_direction > 0 && Opacity >= 1d)
            {
                Opacity = 1;
                Scale = 1;
                //IsHitTestVisible = true;
                _showHideTimer.Stop();
                _displayTimer.Start();
                // Debug.WriteLine($"Fade In complete {Opacity}");
            }
            else if (_direction < 0 && Opacity <= 0)
            {
                _showHideTimer.Stop();
                Opacity = 0;
                Scale = 0;
                //ToastContainer1.IsHitTestVisible = false;
                // Debug.WriteLine($"Fade Out complete {Opacity}");
                OnClose?.Invoke(this);
            }
        }

        private void Display_Tick(object? sender, EventArgs e)
        {
            // Debug.WriteLine($"Display_Tick {Opacity}");
            _displayTimer.Stop();
            if (_direction > 0)
                _direction = -1;
            else
                _direction = 1;
            _showHideTimer.Start();
        }

        public void Close()
        {
            _showHideTimer.Stop();
            _displayTimer.Stop();
            Opacity = 0;
            OnClose?.Invoke(this);
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

    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
