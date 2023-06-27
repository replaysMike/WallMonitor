using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SystemMonitor.Desktop.Models;
using SystemMonitor.Desktop.Views;

namespace SystemMonitor.Desktop.ViewModels
{
    public class ServerPageViewModel : ViewModelBase
    {
        public int PageNumber { get; set; }

        public MainWindowViewModel? Parent { get; set; }

        public SelectionModel<Server> Selection { get; }

        public ObservableCollection<Server> Servers { get; set; }

        public static SpriteSize GlobalDimensions = new SpriteSize(UiConstants.ServerFrameWidth, UiConstants.ServerFrameHeight, UiConstants.ServerFontSize1, UiConstants.ServerFontSize2, UiConstants.ServerTextHeight, new Thickness(0));

        private SpriteSize _dimensions = new SpriteSize(UiConstants.ServerFrameWidth, UiConstants.ServerFrameHeight, UiConstants.ServerFontSize1, UiConstants.ServerFontSize2, UiConstants.ServerTextHeight, new Thickness(0));
        public SpriteSize Dimensions
        {
            get => _dimensions;
            set => this.RaiseAndSetIfChanged(ref _dimensions, value);
        }

        private bool _isCurrentPage;
        public bool IsCurrentPage
        {
            get => _isCurrentPage;
            set
            {
                this.RaiseAndSetIfChanged(ref _isCurrentPage, value);
                HasFailuresAndIsCurrentPage = value && HasFailures;
            }
        }

        private bool _hasFailures;
        public bool HasFailures
        {
            get => _hasFailures;
            set
            {
                this.RaiseAndSetIfChanged(ref _hasFailures, value);
                HasFailuresAndIsCurrentPage = value && IsCurrentPage;
            }
        }

        private bool _hasFailuresAndIsCurrentPage;
        public bool HasFailuresAndIsCurrentPage 
        {
            get => _hasFailuresAndIsCurrentPage;
            set => this.RaiseAndSetIfChanged(ref _hasFailuresAndIsCurrentPage, value);
        }

        public ServerPageViewModel() : this(1, new List<Server>
        {
            new (DesignModeConstants.MonitorId, 1, "DesignModeServer 1", null, 0, true, new List<ServiceState> { new ("HTTP"), new ("ICMP"), new ("DNS") }),
            new (DesignModeConstants.MonitorId, 1, "DesignModeServer 2", null, 1,true, new List<ServiceState> { new ("HTTP"), new ("ICMP", true), new ("DNS") }),
            new (DesignModeConstants.MonitorId, 1, "DesignModeServer 3", null, 2,false, new List<ServiceState> { new ("HTTP"), new ("ICMP", false), new ("DNS") }),
            new (DesignModeConstants.MonitorId, 1, "DesignModeServer 4", null, 3,true, new List<ServiceState> { new ("HTTP"), new ("ICMP"), new ("DNS") }),
            new (DesignModeConstants.MonitorId, 1, "DesignModeServer 5", null, 4,true, new List<ServiceState> { new ("HTTP"), new ("ICMP"), new ("DNS") }),
            new (DesignModeConstants.MonitorId, 1, "DesignModeServer 6", null, 5,true, new List<ServiceState> { new ("HTTP"), new ("ICMP"), new ("DNS") }),
        }, null)
        {
        }

        public ServerPageViewModel(int pageNumber, List<Server> servers, MainWindowViewModel? parent)
        {
            PageNumber = pageNumber;
            Servers = new ObservableCollection<Server>(servers);
            Selection = new SelectionModel<Server>();
            UpdateUiSize(MainWindow.Configuration!.Size);
            if (!Design.IsDesignMode)
            {
                Parent = parent;
                if (Parent != null)
                {
                    Parent.PropertyChanged += Parent_PropertyChanged;
                    IsCurrentPage = Parent.CurrentPage == PageNumber;
                }
            }
        }

        private void Parent_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentPage" && Parent != null)
                IsCurrentPage = Parent.CurrentPage == PageNumber;
            if (e.PropertyName == "Pages")
            {
                HasFailures = Servers.Any(x => x.CurrentState == State.ServerDown || x.CurrentState == State.ParentMonitoringServiceIsDown);
            }
        }

        public void UpdateUiSize(UiSize size)
        {
            GlobalDimensions = Dimensions = GetDimensions(size);
        }

        public static void UpdateGlobalUiSize(UiSize size)
        {
            GlobalDimensions = GetDimensions(size);
        }

        public static SpriteSize GetDimensions(UiSize uiSize)
        {
            switch (uiSize)
            {
                default:
                case UiSize.Small:
                    return new SpriteSize(UiConstants.ServerFrameWidth * 0.68, UiConstants.ServerFrameHeight * 0.68, 14, 8, UiConstants.ServerTextHeight * 0.68, new Thickness(0));
                case UiSize.Normal:
                    return new SpriteSize(UiConstants.ServerFrameWidth, UiConstants.ServerFrameHeight, UiConstants.ServerFontSize1, UiConstants.ServerFontSize2, UiConstants.ServerTextHeight, new Thickness(UiConstants.ServerMargin));
                case UiSize.Large:
                    return new SpriteSize(UiConstants.ServerFrameWidth * 1.37, UiConstants.ServerFrameHeight * 1.37, 24, 16, UiConstants.ServerTextHeight * 1.37, new Thickness(UiConstants.ServerMargin * 2));
                case UiSize.Huge:
                    return new SpriteSize(UiConstants.ServerFrameWidth * 1.71, UiConstants.ServerFrameHeight * 1.71, 36, 18, UiConstants.ServerTextHeight * 1.71, new Thickness(UiConstants.ServerMargin * 2));
            }
        }
    }
}