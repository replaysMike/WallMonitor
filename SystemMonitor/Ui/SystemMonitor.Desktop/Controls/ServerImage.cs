using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SystemMonitor.Desktop.Models;
using SystemMonitor.Resources;

namespace SystemMonitor.Desktop.Controls
{
    public class ServerImage : Control
    {
        private IImage _image;
        private readonly ServerImageAnimationGenerator _animationGenerator;

        public static readonly DirectProperty<ServerImage, bool> IsCheckingProperty =
            AvaloniaProperty.RegisterDirect<ServerImage, bool>(nameof(IsChecking), o => o.IsChecking, (o, value) => o.IsChecking = value);

        public static readonly DirectProperty<ServerImage, bool> IsServerEnabledProperty =
            AvaloniaProperty.RegisterDirect<ServerImage, bool>(nameof(IsServerEnabled), o => o.IsServerEnabled, (o, value) => o.IsServerEnabled = value);

        public static readonly DirectProperty<ServerImage, byte> ImageThemeProperty =
            AvaloniaProperty.RegisterDirect<ServerImage, byte>(nameof(ImageTheme), o => o.ImageTheme, (o, value) => o.ImageTheme = value, 1);

        public static readonly DirectProperty<ServerImage, byte> ImageSizeProperty =
            AvaloniaProperty.RegisterDirect<ServerImage, byte>(nameof(ImageSize), o => o.ImageSize, (o, value) => o.ImageSize = value, 3);

        public static readonly DirectProperty<ServerImage, State> CurrentStateProperty =
            AvaloniaProperty.RegisterDirect<ServerImage, State>(nameof(CurrentState), o => o.CurrentState, (o, value) => o.CurrentState = value, State.ServerIsNotRunning);


        private bool _isChecking = false;
        public bool IsChecking
        {
            get => _isChecking;
            set => SetAndRaise(IsCheckingProperty, ref _isChecking, value);
        }

        private bool _isServerEnabled = false;
        public bool IsServerEnabled
        {
            get => _isServerEnabled;
            set => SetAndRaise(IsServerEnabledProperty, ref _isServerEnabled, value);
        }

        private byte _imageTheme = 1;
        public byte ImageTheme
        {
            get => _imageTheme;
            set
            {
                SetAndRaise(ImageThemeProperty, ref _imageTheme, value);
                if (value <= 0) value = 1;
                _image = new Bitmap(ResourceLoader.LoadStream($"server{_imageSize}u_{value}.png", ResourceType.Sprite, ImageResourceResolution.HD4K));
            }
        }

        private byte _imageSize = 3;
        public byte ImageSize
        {
            get => _imageSize;
            set
            {
                SetAndRaise(ImageSizeProperty, ref _imageSize, value);
                if (value <= 0) value = 3;
                _image = new Bitmap(ResourceLoader.LoadStream($"server{value}u_{_imageTheme}.png", ResourceType.Sprite, ImageResourceResolution.HD4K));
            }
        }

        private State _currentState = State.ServerIsNotRunning;
        public State CurrentState
        {
            get => _currentState;
            set => SetAndRaise(CurrentStateProperty, ref _currentState, value);
        }

        public ServerImage()
        {
            if (_imageSize <= 0) _imageSize = 3;
            if (_imageTheme <= 0) _imageTheme = 1;
            _image = new Bitmap(ResourceLoader.LoadStream($"server{_imageSize}u_{_imageTheme}.png", ResourceType.Sprite, ImageResourceResolution.HD4K));
            _animationGenerator = new(this);;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            InvalidateVisual();
            base.OnPropertyChanged(change);
        }

        public override void Render(DrawingContext context)
        {
            context.DrawImage(_image, new Rect(0, 0, Width, Height));
            
            // draw the server activity LED
            context.DrawEllipse(_isChecking ? (_currentState == State.ServerDown || _currentState == State.ParentMonitoringServiceIsDown ? Brushes.Red : Brushes.Lime) : Brushes.Gray, new Pen(Brushes.Black, 1), new Rect(Width - 8, Height - 12, 5, 5));

            _animationGenerator.RenderAnimationFrame(ImageSize, ImageTheme, Width, Height, context);
        }
    }
}
