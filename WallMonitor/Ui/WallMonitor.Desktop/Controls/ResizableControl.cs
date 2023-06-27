using Avalonia;
using Avalonia.Controls;

namespace WallMonitor.Desktop.Controls
{
    /// <summary>
    /// Provides resize support to a control
    /// </summary>
    public class ResizableControl : UserControl
    {
        private const int DefaultBorderThickness = 4;
        internal bool IsResizing = false;
        private bool _resizeWidth = false;
        private bool _resizeHeight = false;

        public static readonly DirectProperty<ResizableControl, bool> CanResizeProperty = AvaloniaProperty.RegisterDirect<ResizableControl, bool>(nameof(CanResize), o => o.CanResize, (o, value) => o.CanResize = value);
        private bool _canResize = true;
        public bool CanResize
        {
            get => _canResize;
            set => SetAndRaise(CanResizeProperty, ref _canResize, value);
        }

        public ResizableControl()
        {
            PointerMoved += ResizableControl_PointerMoved;
        }

        private void ResizableControl_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (CanResize)
            {
                var pos = e.GetPosition(this);
                var point = e.GetCurrentPoint(this);
                if (IsResizing && point.Properties.IsLeftButtonPressed)
                {
                    var top = Bounds.Top;
                    var left = Bounds.Left;
                    if (_resizeWidth)
                        Width = pos.X;
                    if (_resizeHeight)
                        Height = pos.Y;
                    Canvas.SetLeft(this, left);
                    Canvas.SetTop(this, top);
                }

                if (pos.X >= Bounds.Width - DefaultBorderThickness && pos.Y >= Bounds.Height - DefaultBorderThickness)
                {
                    Cursor = Avalonia.Input.Cursor.Parse("BottomRightCorner");
                    IsResizing = true;
                    _resizeWidth = true;
                    _resizeHeight = true;
                }
                else if (pos.X >= Bounds.Width - DefaultBorderThickness)
                {
                    Cursor = Avalonia.Input.Cursor.Parse("SizeWestEast");
                    IsResizing = true;
                    _resizeWidth = true;
                    _resizeHeight = false;
                }
                else if (pos.Y >= Bounds.Height - DefaultBorderThickness)
                {
                    Cursor = Avalonia.Input.Cursor.Parse("SizeNorthSouth");
                    IsResizing = true;
                    _resizeWidth = false;
                    _resizeHeight = true;
                }
                else
                {
                    Cursor = Avalonia.Input.Cursor.Default;
                    IsResizing = false;
                    _resizeWidth = false;
                    _resizeHeight = false;
                }
            }
        }
    }
}
