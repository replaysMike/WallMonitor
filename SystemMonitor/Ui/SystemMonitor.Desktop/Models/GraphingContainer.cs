using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using SystemMonitor.Common;

namespace SystemMonitor.Desktop.Models
{
    public class GraphingContainer
    {
        private static readonly TimeSpan DataPointLifetime = TimeSpan.FromHours(24);

        public string Name { get; set; }
        public List<DataPoint> Data { get; } = new();

        /// <summary>
        /// Min value (range). If null, auto
        /// </summary>
        public double? MinValue { get; set; }

        /// <summary>
        /// Max value (range). If null, auto
        /// </summary>
        public double? MaxValue { get; set; }

        /// <summary>
        /// The Units to display the values as
        /// </summary>
        public Units Units { get; set; } = Units.Auto;

        /// <summary>
        /// Get/set the graph time scale (0 = realtime, otherwise number of minutes)
        /// </summary>
        public int TimeScale { get; set; } = 0;

        public GraphingContainer(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Add a new data point to the graph
        /// </summary>
        /// <param name="value"></param>
        public void AddDataPoint(double value, bool isUp)
        {
            Data.Add(new DataPoint(value, isUp));
            RemoveOldDataPoints();
        }

        /// <summary>
        /// Set the min/max range of the graph
        /// </summary>
        /// <param name="range"></param>
        public void SetRange(string? range)
        {
            if (!string.IsNullOrEmpty(range) && range.Contains('-'))
            {
                // parse the range
                var parts = range.Split('-');
                if (parts.Length > 0 && double.TryParse(parts[0], out var low))
                    MinValue = low;

                if (parts.Length > 1 && double.TryParse(parts[1], out var high))
                    MaxValue = high;
            }
        }

        /// <summary>
        /// Set the units to display the data as
        /// </summary>
        /// <param name="units"></param>
        public void SetUnits(Units units)
        {
            Units = units;
        }

        private void RemoveOldDataPoints()
        {
            var dataPointsToRemove = Data
                .Where(x => DateTime.UtcNow - x.DateTime > DataPointLifetime)
                .ToList();
            Data.RemoveMany(dataPointsToRemove);
        }
    }

    public class DataPoint
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
        public bool IsUp { get; set; }

        public DataPoint(double value, bool isUp) : this(value, isUp, DateTime.UtcNow) { }

        public DataPoint(double value, bool isUp, DateTime dateTime)
        {
            DateTime = dateTime;
            IsUp = isUp;
            Value = value;
        }

        public override string ToString() => $"{DateTime.TimeOfDay} - {Value}";
    }
}
