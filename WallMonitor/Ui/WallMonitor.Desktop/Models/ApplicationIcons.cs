using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace WallMonitor.Desktop.Models
{
    public static class ApplicationIcons
    {
        /// <summary>
        /// Default application icon (blue)
        /// </summary>
        public static Lazy<WindowIcon> Default => new (() => new WindowIcon(new Bitmap(AvaloniaLocator.Current?.GetService<IAssetLoader>()?.Open(new Uri($"resm:WallMonitor.Desktop.Assets.icon.png")))));

        /// <summary>
        /// Success application icon (green)
        /// </summary>
        public static Lazy<WindowIcon> Success => new (() => new WindowIcon(new Bitmap(AvaloniaLocator.Current?.GetService<IAssetLoader>()?.Open(new Uri($"resm:WallMonitor.Desktop.Assets.icon-green.png")))));

        /// <summary>
        /// Error application icon (red)
        /// </summary>
        public static Lazy<WindowIcon> Error => new (() => new WindowIcon(new Bitmap(AvaloniaLocator.Current?.GetService<IAssetLoader>()?.Open(new Uri($"resm:WallMonitor.Desktop.Assets.icon-red.png")))));

        /// <summary>
        /// Unknown state icon (gray)
        /// </summary>
        public static Lazy<WindowIcon> Unknown => new (() => new WindowIcon(new Bitmap(AvaloniaLocator.Current?.GetService<IAssetLoader>()?.Open(new Uri($"resm:WallMonitor.Desktop.Assets.icon-gray.png")))));
    }
}
