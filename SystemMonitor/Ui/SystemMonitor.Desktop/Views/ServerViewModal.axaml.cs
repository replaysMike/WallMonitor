using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using SystemMonitor.Desktop.Controls;
using SystemMonitor.Desktop.Models;
using SystemMonitor.Desktop.ViewModels;

namespace SystemMonitor.Desktop.Views
{
    public partial class ServerViewModal : ResizableControl
    {
        public ServerViewModalModel ViewModel => (ServerViewModalModel?)DataContext ?? new ServerViewModalModel();
        private Point? _startPoint;
        private bool _isMoving;

        public ServerViewModal()
        {
            InitializeComponent();
            RenderTransform = new ScaleTransform(1, 1);
            if (Design.IsDesignMode)
            {
                var server = new Server(DesignModeConstants.MonitorId, 1, "Test Server", "Server123", 1, true,
                    new List<ServiceState>() { new ServiceState("HTTPS") { Value = 200 }, new ServiceState("ICMP") { Value = 40 }, new ServiceState("DNS") { Value = 51 } });
                DataContext = new ServerViewModalModel(server, new ServiceState("HTTPS") { Value = 200 });
            }
            else
            {
                DataContext = new ServerViewModalModel();
            }
            ServiceList.SelectionChanged += ServiceList_SelectionChanged;
            TimeScale.SelectionChanged += TimeScale_SelectionChanged;
            PropertyChanged += ServerViewModal_PropertyChanged;
            TimeScale.PointerPressed += TimeScale_PointerPressed;
            TimeScale.PointerReleased += TimeScale_PointerReleased;
            PointerMoved += ServerViewModal_PointerMoved;
        }

        private void ServerViewModal_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (_isMoving)
            {
                var pos = e.GetPosition(Parent as Canvas);
                if (_startPoint != null)
                {
                    Canvas.SetLeft(this, pos.X - _startPoint.Value.X);
                    Canvas.SetTop(this, pos.Y - _startPoint.Value.Y);
                }
            }
        }

        private void TimeScale_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            _isMoving = false;
            CanResize = true;
            _startPoint = null;
            Cursor = Avalonia.Input.Cursor.Default;
        }

        private void TimeScale_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            _isMoving = true;
            CanResize = false;
            var pos = e.GetPosition(this);
            _startPoint = pos;
            Cursor = Avalonia.Input.Cursor.Parse("SizeAll");
        }

        private void CloseButton_Clicked(object? sender, RoutedEventArgs? e)
        {
            if (Parent?.Parent?.Parent?.Parent is MainWindow parent)
                parent.RemoveServerModal(this);
        }

        private void ServerViewModal_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name.Equals("IsVisible", StringComparison.InvariantCultureIgnoreCase) || e.Property.Name.Equals("DataContext", StringComparison.InvariantCultureIgnoreCase))
            {
                ServiceList.SelectedIndex = 0;
            }
        }

        private void TimeScale_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // change the graph's time scale
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as TextBlock;
                ViewModel.SelectedTimeScale = int.Parse(item?.Tag?.ToString() ?? "0");
            }
        }

        private void ServiceList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // change the graph to the selected service
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as ServiceState;
                ViewModel.SelectedService = item;
            }
        }

        private void Container_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (!IsResizing && !_isMoving)
            {
                IsVisible = !IsVisible;
                if (IsVisible)
                    RenderTransform = new ScaleTransform(1, 1);
                else
                    RenderTransform = new ScaleTransform(0, 0);
            }
        }
    }
}
