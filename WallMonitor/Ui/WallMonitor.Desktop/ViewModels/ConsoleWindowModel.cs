using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using WallMonitor.Desktop.Models;

namespace WallMonitor.Desktop.ViewModels
{
    public class ConsoleWindowModel : INotifyPropertyChanged
    {
        private const string DateFormat = "ddd, d hh:mm:ss tt";
        private readonly object _dataLock = new object();
        public List<Entry> History = new();
        private ConsoleLogLevel _logLevel = ConsoleLogLevel.Normal;
        public ConsoleLogLevel LogLevel
        {
            get => _logLevel;
            set => SetField(ref _logLevel, value);
        }

        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }

        private double _fontSize = 12;
        public double FontSize
        {
            get => _fontSize;
            set => SetField(ref _fontSize, value);
        }

        public ConsoleWindowModel()
        {

        }

        public void AddHistory(string text, ConsoleLogLevel logLevel = ConsoleLogLevel.Normal, bool displayDate = true, bool update = true)
        {
            lock (_dataLock)
            {
                History.Add(new Entry(text, displayDate, logLevel));
                if (History.Count > 512)
                    History = History.OrderBy(x => x.DateTime).Take(512).ToList();
            }

            if (update)
            {
                var builder = new StringBuilder();
                IEnumerable<Entry> lines = Enumerable.Empty<Entry>();
                lock (_dataLock)
                {
                    lines = History.Where(x => x.LogLevel >= _logLevel).ToList();
                }

                foreach (var line in lines)
                {
                    if (line.DisplayDate)
                        builder.AppendLine($"[{line.DateTime.ToString(DateFormat)}] {line.Text}");
                    else
                        builder.AppendLine($"{line.Text}");
                }

                Text = builder.ToString();
            }

        }

        public void Clear()
        {
            lock (_dataLock)
            {
                History.Clear();
                Text = string.Empty;
            }
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

    public class Entry
    {
        public bool DisplayDate { get; set; }
        public DateTime DateTime { get; set; }
        public string Text { get; set; }
        public ConsoleLogLevel LogLevel { get; set; }

        public Entry(string text) : this(text, true, ConsoleLogLevel.Normal)
        {
        }

        public Entry(string text, bool displayDate, ConsoleLogLevel logLevel)
        {
            DisplayDate = displayDate;
            DateTime = DateTime.UtcNow;
            Text = text;
            LogLevel = logLevel;
        }
    }
}
