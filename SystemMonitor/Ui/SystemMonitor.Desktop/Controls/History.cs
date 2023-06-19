using Avalonia.Threading;
using SystemMonitor.Desktop.Models;
using SystemMonitor.Desktop.Views;

namespace SystemMonitor.Desktop.Controls
{
    public static class History
    {
        private static ConsoleWindow? _target;

        public static void SetTarget(ConsoleWindow target)
        {
            _target = target;
        }

        /// <summary>
        /// Write to the console log
        /// </summary>
        /// <param name="text">The text to log</param>
        public static void Log(string text)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                _target?.ViewModel.AddHistory(text);
                _target?.ScrollToEnd();
            });
        }

        /// <summary>
        /// Write to the console log
        /// </summary>
        /// <param name="text">The text to log</param>
        /// <param name="logLevel"></param>
        public static void Log(string text, ConsoleLogLevel logLevel)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                _target?.ViewModel.AddHistory(text, logLevel);
                _target?.ScrollToEnd();
            });
        }
    }
}
