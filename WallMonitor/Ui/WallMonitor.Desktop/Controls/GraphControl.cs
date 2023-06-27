using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WallMonitor.Desktop.Models;

namespace WallMonitor.Desktop.Controls
{
    public class GraphControl : Control
    {
        private const int GridSize = 10;
        private const int Fps = 2;
        private const int TimeScaleStepSize = 5;
        private readonly TimeSpan _frameRate = TimeSpan.FromSeconds(1d / Fps);
        private readonly LinearGradientBrush _brush;
        private readonly Pen _graphPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 0, 200, 0)), 1);
        private readonly Pen _graphDownPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 200, 0, 0)), 1);
        private readonly Pen _gridPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 12, 18, 34)), 1);
        private FormattedText? _nameText;

        public static readonly DirectProperty<GraphControl, string> LabelProperty = AvaloniaProperty.RegisterDirect<GraphControl, string>(nameof(Label), o => o.Label, (o, value) => o.Label = value);
        private string _label = string.Empty;
        public string Label
        {
            get => _label;
            set
            {
                SetAndRaise(LabelProperty, ref _label, value);
                if (!string.IsNullOrEmpty(value))
                {
                    _nameText = new FormattedText(value, Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 16,
                        new SolidColorBrush(Colors.White, 0.2));
                }
            }
        }

        public GraphControl()
        {
            _brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new (Color.FromArgb(255,0,0,0), 0),
                    new (Color.FromArgb(255,0,0,0), 0.6),
                    new (Color.FromArgb(255, 5, 5, 30), 1),
                }
            };

            var timer = new DispatcherTimer { Interval = _frameRate };
            timer.Tick += Timer_Tick; ;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            context.DrawRectangle(_brush, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

            // draw grid
            for (var x = 0; x < Bounds.Width; x += GridSize)
                context.DrawLine(_gridPen, new Point(x, 0), new Point(x, Bounds.Height));
            for (var y = 0; y < Bounds.Height; y += GridSize)
                context.DrawLine(_gridPen, new Point(0, y), new Point(Bounds.Width, y));

            // draw border
            //context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)), 1), new Rect(0, 0, Bounds.Width - 1, Bounds.Height - 1));

            // draw data
            if (DataContext != null && DataContext is GraphingContainer graphContainer)
            {
                var height = Bounds.Height - 1;
                var width = Bounds.Width - 1;

                // draw name

                if (_nameText != null)
                    context.DrawText(_nameText, new Point(Bounds.Width / 2 - _nameText.Width / 2, 5));

                if (graphContainer.Data.Count < 2)
                    return;
                var numberOfDataPoints = (int)(Bounds.Width / TimeScaleStepSize);

                var timeSpan = GetWindowTime(graphContainer);
                var orderedDataPoints = new List<DataPoint>();
                if (timeSpan == TimeSpan.Zero)
                {
                    // show the last X data points
                    var mostRecentDataPoints = graphContainer.Data
                        .OrderByDescending(x => x.DateTime)
                        .Take(numberOfDataPoints)
                        .ToList();
                    orderedDataPoints.AddRange(mostRecentDataPoints);
                }
                else
                {
                    // compress graph data to timeSpan length

                    var timeBetweenEachDataPoint = timeSpan / numberOfDataPoints;
                    var endTime = DateTime.UtcNow;
                    var startTime = endTime.Subtract(timeSpan);

                    var graphData = graphContainer.Data
                        .OrderByDescending(x => x.DateTime)
                        .Where(x => x.DateTime >= startTime && x.DateTime <= endTime)
                        .ToList();
                    // average all the data between each step size
                    for (var i = 0; i < numberOfDataPoints; i++)
                    {
                        var time = timeBetweenEachDataPoint * i;
                        var dateTime = endTime.Subtract(time);
                        var prevDateTime = endTime.Subtract(timeBetweenEachDataPoint * (i + 1));
                        var values = graphData.Where(x => x.DateTime <= dateTime && x.DateTime > prevDateTime).ToList();
                        var value = values.Any() ? values.Average(x => x.Value) : 0;
                        var isUp = Math.Round(values.Any() ? values.Average(x => x.IsUp ? 1 : 0) : 1);
                        orderedDataPoints.Add(new DataPoint(value, isUp > 0, dateTime));
                    }
                }

                // assume sorted data
                // x = time
                // y = value
                var maxValue = graphContainer.MaxValue ?? (orderedDataPoints.Any() ? orderedDataPoints.Max(x => x.Value) : 0);
                var minValue = graphContainer.MinValue ?? (orderedDataPoints.Any() ? orderedDataPoints.Min(x => x.Value) : 0);
                var x = width;
                var previousDataScaled = 0d;
                foreach (var data in orderedDataPoints)
                {
                    var dataScaled = (data.Value - minValue) / (maxValue - minValue);
                    var y = height - (height * dataScaled);
                    var prevY = height - (height * previousDataScaled);
                    // plot data line
                    if (data.IsUp)
                        context.DrawLine(_graphPen, new Point(x, y), new Point(x + TimeScaleStepSize, prevY));
                    else
                        context.DrawLine(_graphDownPen, new Point(x, y), new Point(x + TimeScaleStepSize, prevY));
                    previousDataScaled = dataScaled;
                    x -= TimeScaleStepSize;
                }
            }
        }

        private TimeSpan GetWindowTime(GraphingContainer graphContainer)
        {
            switch (graphContainer.TimeScale)
            {
                case 0:
                    return TimeSpan.Zero;
                default:
                    return TimeSpan.FromMinutes(graphContainer.TimeScale);
            }
        }
    }
}
