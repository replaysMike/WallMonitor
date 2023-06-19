using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace SystemMonitor.Desktop.Controls
{
    public class ServerImageAnimationGenerator
    {
        private const double OriginalWidth = 709;
        private const double OriginalHeight = 156;
        private const int Fps = 15;
        private byte[] _lights = Array.Empty<byte>();
        private readonly Random _random = new ();
        private readonly DispatcherTimer _timer;
        private int _ticks;
        private readonly TimeSpan _frameRate = TimeSpan.FromSeconds(1d / Fps);
        private readonly Control _parent;
        private Size _size;
        private AnimationType _animationType;
        private readonly Dictionary<AnimationType, (int, int)[]> _lightPos = new()
        {
            { AnimationType.Flutter12, new []{ (161, 147), (191, 147), (223, 147), (256, 147), (289, 147), (321, 147), (354, 147), (386, 147), (418, 147), (451, 147), (484, 147), (516, 147), (549, 147) }},
            { AnimationType.Flutter23, new []{ (158, 117), (318, 117), (475, 117), (633, 117), (158, 143), (318, 143), (475, 143), (633, 143) }},
            { AnimationType.Flutter31, new []{ (189, 52), (343, 52), (497, 52), (652, 52), (189, 90), (343, 90), (497, 90), (652, 90), (189, 129), (343, 129), (497, 129), (652, 129) }},
            { AnimationType.Flutter32, new []
            {
                (84, 49), (107, 49), (129, 49), (151, 49), (173, 49), (196, 49),  (228, 49), (252, 49), (273, 49), (296, 49), (318, 49), (341, 49),  (373, 49), (396, 49), (418, 49), (441, 49), (463, 49), (486, 49),  (517, 49), (540, 49), (563, 49), (585, 49), (608, 49), (631, 49),
                (84, 93), (107, 93), (129, 93), (151, 93), (173, 93), (196, 93),  (228, 93), (252, 93), (273, 93), (296, 93), (318, 93), (341, 93),  (373, 93), (396, 93), (418, 93), (441, 93), (463, 93), (486, 93),  (517, 93), (540, 93), (563, 93), (585, 93), (608, 93), (631, 93),
                (85, 103), (107, 103), (129, 103), (151, 103), (173, 103), (196, 103),  (228, 103), (252, 103), (273, 103), (295, 103), (318, 103), (341, 103),  (373, 103), (396, 103), (418, 103), (441, 103), (463, 103), (486, 103),  (517, 103), (540, 103), (563, 103), (585, 103), (608, 103), (631, 103),
                (85, 149), (108, 149), (130, 149), (152, 149), (173, 149), (196, 149),  (228, 149), (252, 149), (273, 149), (295, 149), (317, 149), (340, 149),  (372, 149), (394, 149), (416, 149), (438, 149), (460, 149), (484, 149),  (515, 149), (537, 149), (560, 149), (582, 149), (604, 149), (627, 149),
            }},
            { AnimationType.Flutter33, new []{ (62, 55), (88, 55), (116, 55), (140, 55), (165, 55), (191, 55), (216, 55), (241, 55), (270, 55), (295, 55), (320, 55), (345, 55), (371, 55), (396, 55), (421, 55), (446, 55), (474, 55), (500, 55), (526, 55), (551, 55), (577, 55), (601, 55), (627, 55), (651, 55) }},
            { AnimationType.Flutter34, new []{ (70, 124), (94, 124), (118, 124), (142, 124), (167, 124), (190, 124), (215, 124), (239, 124), (268, 124), (293, 124), (315, 124), (340, 124), (365, 124), (390, 124), (413, 124), (438, 124), (466, 124), (490, 124), (515, 124), (539, 124), (564, 124), (589, 124), (613, 124), (638, 124) }}
        };

        public ServerImageAnimationGenerator(Control parent)
        {
            _parent = parent;
            _timer = new DispatcherTimer { Interval = _frameRate };
            _timer.Tick += animations_Tick;
        }

        private void SetAnimationType(int imageSize, int imageTheme)
        {
            if (imageSize == 1 && imageTheme == 2)
            {
                if (_animationType != AnimationType.Flutter12)
                {
                    _animationType = AnimationType.Flutter12;
                    _lights = new byte[13];
                    _timer.Start();
                }
            }
            if (imageSize == 2 && imageTheme == 3)
            {
                if (_animationType != AnimationType.Flutter23)
                {
                    _animationType = AnimationType.Flutter23;
                    _lights = new byte[8];
                    _timer.Start();
                }
            }
            if (imageSize == 3 && imageTheme == 1)
            {
                if (_animationType != AnimationType.Flutter31)
                {
                    _animationType = AnimationType.Flutter31;
                    _lights = new byte[12];
                    _timer.Start();
                }
            }
            if (imageSize == 3 && imageTheme == 2)
            {
                if (_animationType != AnimationType.Flutter32)
                {
                    _animationType = AnimationType.Flutter32;
                    _lights = new byte[96];
                    _timer.Start();
                }
            }
            if (imageSize == 3 && imageTheme == 3)
            {
                if (_animationType != AnimationType.Flutter33)
                {
                    _animationType = AnimationType.Flutter33;
                    _lights = new byte[24];
                    _timer.Start();
                }
            }
            if (imageSize == 3 && imageTheme == 4)
            {
                if (_animationType != AnimationType.Flutter34)
                {
                    _animationType = AnimationType.Flutter34;
                    _lights = new byte[24];
                    _timer.Start();
                }
            }
        }

        public void RenderAnimationFrame(int imageSize, int imageTheme, double width, double height, DrawingContext context)
        {
            SetAnimationType(imageSize, imageTheme);
            _size = new Size(width, height);
            switch (_animationType)
            {
                case AnimationType.Flutter12:
                    RenderLights(context, Brushes.YellowGreen, 2);
                    break;
                case AnimationType.Flutter23:
                    RenderLights(context, Brushes.Lime, 4);
                    break;
                case AnimationType.Flutter31:
                    RenderLights(context, Brushes.Lime, 2);
                    break;
                case AnimationType.Flutter32:
                    RenderLights(context, Brushes.YellowGreen, 2);
                    break;
                case AnimationType.Flutter33:
                    RenderLights(context, Brushes.Cyan, 4);
                    break;
                case AnimationType.Flutter34:
                    RenderLights(context, Brushes.Pink, 2);
                    break;
            }
        }

        private void RenderLights(DrawingContext context, IImmutableSolidColorBrush brush, double radius)
        {
            for (var i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] > 0)
                {
                    var scaledSize = Scale(_lightPos[_animationType][i].Item1, _lightPos[_animationType][i].Item2, radius);
                    context.DrawEllipse(brush, null, new Rect(scaledSize.Width, scaledSize.Height, radius, radius));
                }
            }
        }

        private Size Scale(double x, double y, double radius) 
            => new (x * (_size.Width / OriginalWidth) - radius / 2, y * (_size.Height / OriginalHeight) - radius / 2);

        private void animations_Tick(object? sender, EventArgs e)
        {
            _ticks++;
            SetLightFrequency();
        }

        private void SetLightFrequency()
        {
            switch (_animationType)
            {
                case AnimationType.Flutter12:
                    // random drive lights
                    for (var i = 0; i < _lights.Length; i++)
                        _lights[i] = (byte)(_random.Next(666) % 2 == 0 ? 1 : 0);
                    _parent.InvalidateVisual();
                    break;
                case AnimationType.Flutter23:
                    // random drive lights
                    for (var i = 0; i < _lights.Length; i++)
                        _lights[i] = (byte)(_random.Next(999) % 100 == 0 ? 1 : 0);
                    _parent.InvalidateVisual();
                    break;
                case AnimationType.Flutter31:
                    // random drive lights
                    for (var i = 0; i < _lights.Length; i++)
                        _lights[i] = (byte)(_random.Next(800) % 2 == 0 ? 1 : 0);
                    _parent.InvalidateVisual();
                    break;
                case AnimationType.Flutter32:
                    // random drive lights
                    for (var i = 0; i < _lights.Length; i++)
                        _lights[i] = (byte)(_random.Next(800) % 2 == 0 ? 1 : 0);
                    _parent.InvalidateVisual();
                    break;
                case AnimationType.Flutter33:
                    // random drive lights
                    for (var i = 0; i < _lights.Length; i++)
                        _lights[i] = (byte)(_random.Next(999) % 150 == 0 ? 1 : 0);
                    _parent.InvalidateVisual();
                    break;
                case AnimationType.Flutter34:
                    // random drive lights
                    for (var i = 0; i < _lights.Length; i++)
                        _lights[i] = (byte)(_random.Next(999) % 150 == 0 ? 1 : 0);
                    _parent.InvalidateVisual();
                    break;
                default:
                case AnimationType.None:
                    break;
            }
        }

        private enum AnimationType
        {
            None,
            Flutter12,
            Flutter23,
            Flutter31,
            Flutter32,
            Flutter33,
            Flutter34,
        }
    }
}
