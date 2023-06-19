using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SystemMonitor.Desktop.Controls
{
    public class ServerHealthBar : Control
    {
        private readonly LinearGradientBrush _brush;
        public ServerHealthBar()
        {
            _brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
            };
        }

        public override void Render(DrawingContext context)
        {
            _brush.GradientStops = new GradientStops
            {
                new (Color.FromArgb(255,0,150,0), 0),
                new (Color.FromArgb(255, 0, 200, 0), 1),
            };
            context.DrawRectangle(_brush, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }
    }
}
