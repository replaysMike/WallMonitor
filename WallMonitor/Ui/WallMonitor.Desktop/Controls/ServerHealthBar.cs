using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using WallMonitor.Desktop.Models;
using WallMonitor.Resources;

namespace WallMonitor.Desktop.Controls
{
    public class ServerHealthBar : Control
    {
        public static readonly DirectProperty<ServerHealthBar, byte> ImageThemeProperty =
            AvaloniaProperty.RegisterDirect<ServerHealthBar, byte>(nameof(ImageTheme), o => o.ImageTheme, (o, value) => o.ImageTheme = value, 1);

        public static readonly DirectProperty<ServerHealthBar, byte> ImageSizeProperty =
            AvaloniaProperty.RegisterDirect<ServerHealthBar, byte>(nameof(ImageSize), o => o.ImageSize, (o, value) => o.ImageSize = value, 1);

        private byte _imageTheme = UiConstants.MinTheme;
        public byte ImageTheme
        {
            get => _imageTheme;
            set
            {
                if (value > UiConstants.MaxTheme) value = UiConstants.MaxTheme;
                SetAndRaise(ImageThemeProperty, ref _imageTheme, value);
            }
        }

        private byte _imageSize = UiConstants.MinImageSize;
        public byte ImageSize
        {
            get => _imageSize;
            set
            {
                if (value > UiConstants.MaxImageSize) value = UiConstants.MaxImageSize;
                SetAndRaise(ImageSizeProperty, ref _imageSize, value);
            }
        }

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
            if (_imageSize > 0 && _imageTheme > 0)
            {
                _brush.GradientStops = new GradientStops
                {
                    new(Color.FromArgb(255, 0, 150, 0), 0),
                    new(Color.FromArgb(255, 0, 200, 0), 1),
                };
                context.DrawRectangle(_brush, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
            }
        }
    }
}
