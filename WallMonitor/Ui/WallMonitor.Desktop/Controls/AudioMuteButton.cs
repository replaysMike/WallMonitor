using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using WallMonitor.Desktop.Services;
using WallMonitor.Resources;

namespace WallMonitor.Desktop.Controls
{
    public class AudioMuteButton : Button
    {
        private readonly IImage _audioUnmutedImage;
        private readonly IImage _audioMutedImage;
        private const double AspectRatio = 1;
        private double _previousOpacity;

        public static readonly DirectProperty<AudioMuteButton, bool> IsMutedProperty =
            AvaloniaProperty.RegisterDirect<AudioMuteButton, bool>(nameof(IsMuted), o => o.IsMuted, (o, value) => o.IsMuted = value);

        private bool _isMuted = false;
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                SetAndRaise(IsMutedProperty, ref _isMuted, value);
                InvalidateVisual();
            }
        }

        public AudioMuteButton()
        {
            IsHitTestVisible = true;
            PressedMixin.Attach<AudioMuteButton>();
            _audioUnmutedImage = new Bitmap(ResourceLoader.LoadStream($"audio_button.png", ResourceType.Sprite, ImageResourceResolution.HD4K));
            _audioMutedImage = new Bitmap(ResourceLoader.LoadStream($"audio_button_muted.png", ResourceType.Sprite, ImageResourceResolution.HD4K));
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            IsMuted = !IsMuted;
            AudioService.Instance.IsMuted = IsMuted;
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            _previousOpacity = Opacity;
            Opacity = 1;
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            Opacity = _previousOpacity;
        }

        public override void Render(DrawingContext context)
        {
            if (_isMuted)
            {
                context.DrawImage(_audioMutedImage, new Avalonia.Rect(0, 0, Width, double.IsNaN(Height) ? Width * AspectRatio : Height));
            }
            else
            {
                context.DrawImage(_audioUnmutedImage, new Avalonia.Rect(0, 0, Width, double.IsNaN(Height) ? Width * AspectRatio : Height));
            }
        }
    }
}
