using Avalonia.Threading;
using System;
using WallMonitor.Desktop.Views;

namespace WallMonitor.Desktop.Controls
{
    public static class Toast
    {
        private static ToastContainer? _target;

        public static void SetTarget(ToastContainer target)
        {
            _target = target;
        }

        /// <summary>
        /// Show an info toast
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void Info(string message)
        {
            Dispatcher.UIThread.Invoke(() => _target?.Show(ToastType.Info, message));
        }

        /// <summary>
        /// Show an info toast
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="duration">The duration to display the toast for</param>
        public static void Info(string message, TimeSpan duration)
        {
            Dispatcher.UIThread.Invoke(() => _target?.Show(ToastType.Info, message, duration));
        }

        /// <summary>
        /// Show a success toast
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void Success(string message)
        {
            Dispatcher.UIThread.Invoke(() => _target?.Show(ToastType.Success, message));
        }

        /// <summary>
        /// Show a success toast
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="duration">The duration to display the toast for</param>
        public static void Success(string message, TimeSpan duration)
        {
            Dispatcher.UIThread.Invoke(() => _target?.Show(ToastType.Success, message, duration));
        }

        /// <summary>
        /// Show a warning toast
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void Warning(string message)
        {
            Dispatcher.UIThread.Invoke(() => _target?.Show(ToastType.Warning, message));
        }

        /// <summary>
        /// Show a warning toast
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="duration">The duration to display the toast for</param>
        public static void Warning(string message, TimeSpan duration)
        {
            Dispatcher.UIThread.Invoke(() => _target?.Show(ToastType.Warning, message, duration));
        }

        /// <summary>
        /// Show an error toast
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void Error(string message)
        {
            Dispatcher.UIThread.Invoke(() => _target?.Show(ToastType.Error, message));
        }

        /// <summary>
        /// Show an error toast
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="duration">The duration to display the toast for</param>
        public static void Error(string message, TimeSpan duration)
        {
            Dispatcher.UIThread.Invoke(() => _target?.Show(ToastType.Error, message, duration));
        }

        /// <summary>
        /// Clear all toasts
        /// </summary>
        public static void Clear()
        {
            Dispatcher.UIThread.Invoke(() => _target?.Clear());
        }
    }
}
