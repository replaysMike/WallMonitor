using Avalonia;

namespace WallMonitor.Desktop.Models
{
    public class SpriteSize
    {
        /// <summary>
        /// Size of the sprite's frame
        /// </summary>
        public Size FrameSize { get; set; }

        /// <summary>
        /// Size of the sprite's server image
        /// </summary>
        public Size ServerSize { get; set; }

        public double TextHeight { get; set; }

        /// <summary>
        /// Font size
        /// </summary>
        public double FontSize1 { get; set; }

        /// <summary>
        /// Font size
        /// </summary>
        public double FontSize2 { get; set; }

        /// <summary>
        /// Server bounds margin
        /// </summary>
        public Thickness Margin { get; set; }

        public SpriteSize(double frameWidth, double frameHeight, double fontSize1, double fontSize2, double textHeight, Thickness margin) : this(frameWidth, frameHeight, fontSize1, fontSize2, textHeight, margin, frameWidth - 10, (frameWidth - 10) / 4.54d) { }

        public SpriteSize(double frameWidth, double frameHeight, double fontSize1, double fontSize2, double textHeight, Thickness margin, double serverWidth, double serverHeight)
        {
            FrameSize = new Size(frameWidth, frameHeight);
            ServerSize = new Size(serverWidth, serverHeight);
            TextHeight = textHeight;
            FontSize1 = fontSize1;
            FontSize2 = fontSize2;
            Margin = margin;
        }
    }
}
