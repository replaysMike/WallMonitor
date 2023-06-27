using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using WallMonitor.Resources;

namespace WallMonitor.Desktop.Controls
{
    public class LoadingIcon : Control
    {
        private Dictionary<string, IImage> _images = new ();

        public LoadingIcon()
        {
            _images.Add("server", new Bitmap(ResourceLoader.LoadStream("server1.png", ResourceType.Sprite, ImageResourceResolution.HD4K)));
        }

        public override void Render(DrawingContext context)
        {
            context.DrawImage(GetImage("server"), new Avalonia.Rect(0, 0, Width, Height));
        }

        private IImage GetImage(string name)
        {
            return _images[name];
        }
    }
}
