using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using WallMonitor.Common;
using WallMonitor.Common.IO;
using WallMonitor.Common.Models;
using WallMonitor.Desktop.Controls;
using WallMonitor.Desktop.Models;
using WallMonitor.Desktop.Services;
using WallMonitor.Desktop.ViewModels;

namespace WallMonitor.Desktop.Views
{
    public partial class MainWindow : Window
    {
        internal readonly List<Server> _allServers = new();
        public MainWindowViewModel ViewModel => (MainWindowViewModel?)DataContext ?? new MainWindowViewModel();
        private const int TopMargin = 20;
        internal static MonitoringServicesConfiguration? Configuration;
        private int _downHostsCount;
        private int _downServicesCount;
        private DateTime _lastDownAudioAlert = DateTime.MinValue;
        private DateTime _lastUpAudioAlert = DateTime.MinValue;
        private TimeSpan _longestDownTime = TimeSpan.Zero;
        private bool _nonStandardSortApplied = false;
        private DispatcherTimer _cyclePagesTimer;

        public MainWindow()
        {
            DataContext = new MainWindowViewModel();
        }

        public MainWindow(IMessageNotificationService messageNotificationService, MonitoringServicesConfiguration configuration)
        {
            InitializeComponent();
            var messageNotificationService1 = messageNotificationService;
            Configuration = configuration;
            messageNotificationService1.OnReceiveServerMessage += OnReceiveServerMessage;
            messageNotificationService1.OnReceiveMonitoringServiceMessage += OnReceiveServiceMessage;
            BackgroundCanvas.SetMessageNotificationService(messageNotificationService);

            // _allServers = CreateTestData();

            DataContext = new MainWindowViewModel();
            if (!Design.IsDesignMode)
            {
                // resize the listview when the window changes
                ClientSizeProperty.Changed.Subscribe(size =>
                {
                    BuildPagesForScreenArea(size.NewValue.Value);

                    PaginatedView1.Width = BackgroundCanvas.Width = size.NewValue.Value.Width;
                    PaginatedView1.Height = BackgroundCanvas.Height = size.NewValue.Value.Height - TopMargin;
                    InvalidateVisual();
                });
            }

            Toast.SetTarget(ToastContainer1);

            ConsoleWindowOverlay.DataContext = new ConsoleWindowModel();
            History.SetTarget(ConsoleWindowOverlay);
            History.Log($"Welcome to System Monitor v{Versioning.GetCurrentVersion()}", ConsoleLogLevel.None);
            History.Log("Type /help for a list of commands.", ConsoleLogLevel.None);

            AddHandler(InputElement.KeyDownEvent, HandleOnKeyDown, Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
            AudioService.Instance.OnMuteChanged += Instance_OnMuteChanged;

            _cyclePagesTimer = new DispatcherTimer();
            _cyclePagesTimer.Tick += CyclePagesTimer_Tick;
            if (configuration.CyclePages)
            {
                if (configuration.CycleInterval == TimeSpan.Zero)
                    configuration.CycleInterval = TimeSpan.FromSeconds(1);
                _cyclePagesTimer.Interval = configuration.CycleInterval;
                _cyclePagesTimer.Start();
            }
        }

        private void CyclePagesTimer_Tick(object? sender, EventArgs e)
        {
            if (PaginatedView1.ViewModel.PageCount > 1)
            {
                var nextPage = PaginatedView1.CurrentPage + 1;
                if (nextPage > PaginatedView1.ViewModel.PageCount)
                    nextPage = 1;
                PaginatedView1.SwitchToPage(nextPage);
            }
        }

        public void EnableCyclePages()
        {
            _cyclePagesTimer.Start();
        }

        public void DisableCyclePages()
        {
            _cyclePagesTimer.Stop();
        }

        public void SetCyclePagesInterval(TimeSpan interval)
        {
            _cyclePagesTimer.Interval = interval;
        }

        public void AddServerViewModal(Server? server, Point? point)
        {
            if (server != null && point != null)
            {
                if (!ControlPanel.Children.Any(x => x.Name == server.Name && x.GetType() == typeof(Canvas)))
                {
                    var canvas = new Canvas
                    {
                        Name = server.Name
                    };
                    var modal = new ServerViewModal
                    {
                        Name = server.Name,
                        DataContext = new ServerViewModalModel(server, server.Services.First()),
                        Width = 300,
                        Height = 200
                    };
                    Canvas.SetLeft(modal, point.Value.X);
                    Canvas.SetTop(modal, point.Value.Y);
                    canvas.Children.Add(modal);
                    ControlPanel.Children.Add(canvas);
                }
            }
        }

        public void RemoveServerModal(ServerViewModal modal)
        {
            if (modal.Parent is Canvas child)
                ControlPanel.Children.Remove(child);
        }

        /// <summary>
        /// Update the server configuration from a network message received from a messaging service
        /// </summary>
        /// <param name="monitorId"></param>
        /// <param name="monitorOrderId"></param>
        /// <param name="configuration"></param>
        public void UpdateServerConfiguration(Guid monitorId, int monitorOrderId, LimitedConfiguration configuration)
        {
            //Debug.WriteLine($"Received Server Configuration for monitor {monitorId}!");
            //History.Log($"Received Server Configuration for monitor {configuration.Monitor}!");

            foreach (var host in configuration.Hosts)
            {
                var serviceStates = host.Services.Select(x => new Models.ServiceState(x.Name, x.Enabled)).ToList();
                if (!_allServers.Any(x => x.MonitorId == monitorId && x.Name == host.Name))
                {
                    // add new server
                    var server = new Server(monitorId, monitorOrderId, host.Name, host.HostName, host.OrderId, host.Enabled, serviceStates);
                    server.ImageTheme = host.ImageTheme;
                    server.ImageSize = host.ImageSize;
                    _allServers.Add(server);
                }
                else
                {
                    // update existing server
                    var server = _allServers.First(x => x.MonitorId == monitorId && x.Name == host.Name);
                    server.HostName = host.HostName;
                    server.OrderId = host.OrderId;
                    server.IsEnabled = host.Enabled;
                    foreach (var service in serviceStates)
                    {
                        // update/add existing services
                        var existingService = server.Services.FirstOrDefault(x => x.Name.Equals(service.Name));
                        if (existingService == null)
                        {
                            server.Services.Add(service);
                        }
                        else
                        {
                            // update it's properties from the configuration
                            existingService.CurrentState = service.CurrentState;
                            existingService.GraphType = service.GraphType;
                            existingService.IsEnabled = service.IsEnabled;
                            existingService.Range = service.Range;
                            existingService.TimeScale = service.TimeScale;
                            existingService.Units = service.Units;
                            // don't update these properties as it may have existing data
                            //existingService.Data = service.Data;
                            //existingService.IsChecking = service.IsChecking;
                            //existingService.IsDown = service.IsDown;
                            //existingService.LastChecked = service.LastChecked;
                            //existingService.ResponseTime = service.ResponseTime;
                            //existingService.ResponseTimeFormatted = service.ResponseTimeFormatted;
                            //existingService.Value = service.Value;
                            //existingService.ValueFormatted = service.ValueFormatted;
                        }
                    }
                    // delete non-existent services
                    var servicesToDelete = server.Services.Where(x => !serviceStates.Contains(x)).ToList();
                    if (servicesToDelete.Any())
                    {
                        server.Services.RemoveMany(servicesToDelete);
                    }
                }
            }


            // delete non-existent hosts
            var hostsToDelete = new List<Server>();
            foreach (var server in _allServers.Where(x => x.MonitorId == monitorId))
            {
                if (!configuration.Hosts.Any(x => x.Name.Equals(server.Name)))
                {
                    hostsToDelete.Add(server);
                }
            }
            if (hostsToDelete.Any())
            {
                _allServers.RemoveMany(hostsToDelete);
            }

            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    BuildPagesForScreenArea(ClientSize);
                    WaitOverlay.Opacity = 0;
                    WaitOverlay.IsHitTestVisible = false;
                    InvalidateVisual();
                });
            }
            catch (TaskCanceledException)
            {
            }
        }

        /// <summary>
        /// Assign servers to pages
        /// </summary>
        /// <param name="size"></param>
        /// <param name="forceUpdate"></param>
        /// <param name="orderPredicate"></param>
        /// <param name="orderDirection"></param>
        public void BuildPagesForScreenArea<TKey>(Size size, bool forceUpdate = false, Func<Server, TKey>? orderPredicate = null, OrderDirection? orderDirection = null)
        {
            //Debug.WriteLine("BuildPagesForScreenArea", size.ToString());
            var width = size.Width;
            var height = size.Height - TopMargin - PaginatedView.PaginationHeight;

            var serverFrameWidth = ServerPageViewModel.GlobalDimensions.FrameSize.Width;
            var serverFrameHeight = ServerPageViewModel.GlobalDimensions.FrameSize.Height;
            var itemsPerRow = (int)Math.Floor(width / serverFrameWidth);
            var itemsPerColumn = (int)Math.Floor(height / serverFrameHeight);
            var totalServersPerPage = (int)Math.Floor(itemsPerRow * (double)itemsPerColumn);
            var totalPages = (int)Math.Ceiling(_allServers.Count / (double)totalServersPerPage);

            var index = 0;
            // view optimization: only change the data context if there is a different number of servers on the page
            if (forceUpdate || _allServers.Count != ViewModel.TotalServers || ViewModel.ServersPerRow != itemsPerRow || ViewModel.ServersPerColumn != itemsPerColumn)
            {
                var newPages = new List<ServerPageViewModel>();
                List<Server> allServersOrdered;
                if (orderPredicate != null)
                {
                    allServersOrdered = _allServers.OrderBy(x => x.MonitorOrderId).ThenBy(orderPredicate).ToList();
                }
                else
                {
                    // order by configured property
                    switch (Configuration?.OrderBy ?? OrderBy.DefinedByService)
                    {
                        case OrderBy.DefinedByService:
                        default:
                            allServersOrdered = _allServers.OrderBy(x => x.MonitorOrderId).ThenBy(x => x.OrderId).ToList();
                            break;
                        case OrderBy.DisplayName:
                            allServersOrdered = _allServers.OrderBy(x => x.MonitorOrderId).ThenBy(x => x.Name).ToList();
                            break;
                        case OrderBy.Hostname:
                            allServersOrdered = _allServers.OrderBy(x => x.MonitorOrderId).ThenBy(x => x.HostName).ToList();
                            break;
                    }
                }

                if ((orderDirection != null && orderDirection == OrderDirection.Descending) || Configuration?.OrderDirection == OrderDirection.Descending)
                    allServersOrdered.Reverse();

                for (var i = 0; i < totalPages; i++)
                {
                    var serversOnPage = new List<Server>();
                    for (var s = 0; s < totalServersPerPage; s++)
                    {
                        if (index < _allServers.Count)
                        {
                            serversOnPage.Add(allServersOrdered[index]);
                            index++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    var page = new ServerPageViewModel(i, serversOnPage, ViewModel);
                    newPages.Add(page);
                }

                ViewModel.Pages = newPages;
                ViewModel.ServersPerRow = itemsPerRow;
                ViewModel.ServersPerColumn = itemsPerColumn;
                ViewModel.TotalServers = _allServers.Count;
            }

            //Debug.WriteLine($"Layout is {totalPages} pages with {totalServersPerPage} servers per page.");
        }

        public void BuildPagesForScreenArea(Size size, bool forceUpdate = false)
        {
            BuildPagesForScreenArea<object>(size, forceUpdate, null, null);
        }

        private void Instance_OnMuteChanged(object sender, AudioService.MuteChangedEventArgs e)
        {
            // update the icon
            AudioMuteButton1.IsMuted = e.NewValue;
        }

        private void OnReceiveServerMessage(object sender, ServerMessageEventArgs e)
        {
            if (e.Message.EventType == EventType.ServiceOffline)
            {
                // the monitoring service is offline, update every server that it monitors as being of unknown state.
                History.Log($"Server message: {e.Message.Message}!");
                TriggerDownAudio();
                Dispatcher.UIThread.Invoke(() =>
                {
                    foreach (var server in _allServers.Where(x => x.MonitorId == e.Message.MonitorId))
                    {
                        ViewModel.UpdateServer(server.Name, State.ParentMonitoringServiceIsDown);
                    }
                    ApplySortingToDisplayFailedServers();
                    //BuildPagesForScreenArea(ClientSize, true);
                    InvalidateVisual();
                });

                Toast.Error(e.Message.Message);
                Dispatcher.UIThread.Invoke(() => { Icon = ApplicationIcons.Error.Value; });
                UpdateDownServerCount();
            }

            if (e.Message.EventType == EventType.ServiceRestored)
            {
                // the monitoring service is back online
                History.Log($"Server message: {e.Message.Message}!");
                TriggerUpAudio();

                Dispatcher.UIThread.Invoke(() =>
                {
                    foreach (var server in _allServers.Where(x => x.MonitorId == e.Message.MonitorId))
                    {
                        // default to isRunning, if it's down another event will update it.
                        ViewModel.UpdateServer(server.Name, State.ServerIsRunning);
                    }
                    ApplySortingToDisplayFailedServers();
                    //BuildPagesForScreenArea(ClientSize, true);
                    InvalidateVisual();
                });

                Toast.Success(e.Message.Message);
                Dispatcher.UIThread.Invoke(() => { Icon = ApplicationIcons.Success.Value; });
                UpdateDownServerCount();
            }
        }

        /// <summary>
        /// Receive an update message sent by a messaging service for a particular service
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReceiveServiceMessage(object sender, MonitoringServiceEventArgs e)
        {
            try
            {
                var message = e.Message;

                var displayEvent = false;
                var state = SystemAlertLevel.Info;
                var displayMessage = "";
                switch (message.EventType)
                {
                    case Common.IO.EventType.HostCheckFailed:
                        displayEvent = true;
                        state = SystemAlertLevel.Error;
                        if (message.ServiceName == "*")
                        {
                            displayMessage = $"All services on {message.ServerName} are down.";
                            _downHostsCount++;
                        }
                        else
                        {
                            displayMessage = $"{message.ServiceName} on {message.ServerName} is down.";
                            _downServicesCount++;
                        }

                        var downTime = DateTime.UtcNow.Subtract(message.LastUpTime);
                        displayMessage += $"\nDown for {Util.GetFriendlyElapsedTime(downTime)}";
                        if (downTime > _longestDownTime)
                            _longestDownTime = downTime;

                        History.Log($"{message.ServerName} {message.EventType} - {displayMessage.Replace("\n", " ")}!");
                        TriggerDownAudio();

                        Dispatcher.UIThread.Invoke(() => ViewModel.UpdateServer(message.ServerName, message.ServiceName, State.ServerDown, message.Value, message.Range, message.Units, message.ResponseTime, message.GraphType));
                        ApplySortingToDisplayFailedServers();
                        UpdateDownServerCount();
                        break;
                    case Common.IO.EventType.HostCheckRecovered:
                        displayEvent = true;
                        state = SystemAlertLevel.Success;
                        if (message.ServiceName == "*")
                        {
                            displayMessage = $"All services on {message.ServerName} have recovered!";
                            _downHostsCount--;
                        }
                        else
                        {
                            displayMessage = $"{message.ServiceName} on {message.ServerName} has recovered!";
                            _downServicesCount--;
                        }

                        if (message.PreviousDownTime.TotalMilliseconds > 0)
                            displayMessage += $"\nDown time was {Util.GetFriendlyElapsedTime(message.PreviousDownTime)}";
                        if (_downHostsCount == 0 && _downServicesCount == 0)
                            _longestDownTime = TimeSpan.Zero;

                        History.Log($"{message.ServiceName} {message.ServerName} {message.EventType} - {displayMessage.Replace("\n", " ")}!");
                        TriggerUpAudio();

                        Dispatcher.UIThread.Invoke(() => ViewModel.UpdateServer(message.ServerName, message.ServiceName, State.ServerIsRunning, message.Value, message.Range, message.Units, message.ResponseTime, message.GraphType));
                        //ApplySortingToDisplayFailedServers();
                        UpdateDownServerCount();
                        break;
                    case Common.IO.EventType.HostCheckStarted:
                        state = SystemAlertLevel.Info;
                        displayMessage = $"{message.ServiceName} on {message.ServerName} has started check.";
                        History.Log($"{message.ServerName} {message.EventType} - {displayMessage.Replace("\n", " ")}!", ConsoleLogLevel.Verbose);
                        Dispatcher.UIThread.Invoke(() => ViewModel.UpdateServer(message.ServerName, message.ServiceName, true, message.Value, message.Range, message.Units, message.ResponseTime, message.GraphType));
                        break;
                    case Common.IO.EventType.HostCheckCompleted:
                        // displayEvent = true;
                        state = SystemAlertLevel.Info;
                        displayMessage = $"{message.ServiceName} on {message.ServerName} has completed check.";
                        History.Log($"{message.ServerName} {message.EventType} - {displayMessage.Replace("\n", " ")}!", ConsoleLogLevel.Verbose);
                        Dispatcher.UIThread.Invoke(() => ViewModel.UpdateServer(message.ServerName, message.ServiceName, message.State == Common.IO.ServiceState.Down ? State.ServerDown : State.ServerIsRunning, false, message.Value, message.Range, message.Units, message.ResponseTime, message.GraphType));
                        break;
                }

                // display toast messages by state
                if (displayEvent)
                {
                    switch (state)
                    {
                        case SystemAlertLevel.Info:
                            Toast.Info(displayMessage);
                            break;
                        case SystemAlertLevel.Success:
                            Toast.Success(displayMessage);
                            Dispatcher.UIThread.Invoke(() => { Icon = ApplicationIcons.Success.Value; });
                            break;
                        case SystemAlertLevel.Warning:
                            Toast.Warning(displayMessage);
                            break;
                        case SystemAlertLevel.Error:
                            Toast.Error(displayMessage);
                            Dispatcher.UIThread.Invoke(() => { Icon = ApplicationIcons.Error.Value; });
                            break;
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Toast.Error($"Exception: {ex.GetBaseException().Message}");
            }
        }

        private void UpdateDownServerCount()
        {
            var downServersCount = _allServers.Count(x => x.CurrentState == State.ServerDown || x.CurrentState == State.ParentMonitoringServiceIsDown);
            Dispatcher.UIThread.Invoke(() =>
            {
                StatusCountButton1.Count = downServersCount;
            });
        }

        private void ApplySortingToDisplayFailedServers()
        {
            var failuresOnPages = GetFailures();
            var pagesWithFailures = failuresOnPages.Count(x => x.Value > 0);
            if (pagesWithFailures == 0 && _nonStandardSortApplied)
            {
                // reset the sort to default/configured value
                _nonStandardSortApplied = false;
                Dispatcher.UIThread.Invoke(() =>
                {
                    BuildPagesForScreenArea(ClientSize, true);
                    if (PaginatedView1.CurrentPage != 0 && ViewModel.AllowAutoPageNavigation)
                        PaginatedView1.SwitchToPage(0);
                    InvalidateVisual();
                });
            }
            if (pagesWithFailures > 1)
            {
                _nonStandardSortApplied = true;
                // re-sort all items to show down servers on same page
                Dispatcher.UIThread.Invoke(() =>
                {
                    BuildPagesForScreenArea(ClientSize, true, x => x.CurrentState, OrderDirection.Ascending);
                    if (PaginatedView1.CurrentPage != 0 && ViewModel.AllowAutoPageNavigation)
                        PaginatedView1.SwitchToPage(0);
                    InvalidateVisual();
                });
            }
            else if (pagesWithFailures == 1)
            {
                // switch to page
                var pageNumber = failuresOnPages.First(x => x.Value > 0).Key;
                Dispatcher.UIThread.Invoke(() =>
                {
                    Debug.WriteLine($"ApplySortingToDisplayFailedServers {PaginatedView1.CurrentPage}");
                    if (PaginatedView1.CurrentPage != pageNumber && ViewModel.AllowAutoPageNavigation)
                    {
                        PaginatedView1.SwitchToPage(pageNumber);
                        InvalidateVisual();
                    }
                });
            }
        }

        private Dictionary<int, int> GetFailures()
        {
            var failuresOnPages = new Dictionary<int, int>();
            // count the failures on each page number
            Dispatcher.UIThread.Invoke(() =>
            {
                foreach (var page in ViewModel.Pages)
                {
                    failuresOnPages.Add(page.PageNumber, page.Servers.Count(x => x.CurrentState == State.ServerDown || x.CurrentState == State.ParentMonitoringServiceIsDown));
                }
            });
            return failuresOnPages;
        }

        private void TriggerDownAudio()
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                AudioMuteButton1.Opacity = 1;
            });
            if (Configuration?.AudioAlerts == true)
            {
                // vary how often audio alerts are triggered
                var playEvery = TimeSpan.FromSeconds(15);
                if (_longestDownTime.TotalMinutes >= 1 && _longestDownTime.TotalMinutes < 4)
                    playEvery = TimeSpan.FromSeconds(30);
                else if (_longestDownTime.TotalMinutes >= 4 && _longestDownTime.TotalMinutes < 8)
                    playEvery = TimeSpan.FromMinutes(1);
                else if (_longestDownTime.TotalMinutes >= 8 && _longestDownTime.TotalMinutes < 15)
                    playEvery = TimeSpan.FromMinutes(2);
                else if (_longestDownTime.TotalMinutes >= 15)
                    playEvery = TimeSpan.FromMinutes(3);
                //Debug.WriteLine($"Audio alert will play every ${playEvery}");

                if (DateTime.UtcNow.Subtract(_lastDownAudioAlert) >= playEvery)
                {
                    if (Configuration.ProgressiveAudio)
                    {
                        // vary the alert type the more downtime there is
                        if (_longestDownTime.TotalMinutes <= 1)
                        {
                            AudioService.Instance.PlayServiceDown(1);
                        }
                        else if (_longestDownTime.TotalMinutes <= 3)
                        {
                            AudioService.Instance.PlayServiceDown(2);
                        }
                        else if (_longestDownTime.TotalMinutes <= 5 && Configuration.AudioAlertLevel >= AudioAlertLevel.Normal)
                        {
                            AudioService.Instance.PlayServiceDown(3);
                        }
                        else if (Configuration.AudioAlertLevel == AudioAlertLevel.Obnoxious)
                        {
                            AudioService.Instance.PlayServiceDown(4);
                        }
                    }
                    else
                    {
                        // play the quietest alert
                        AudioService.Instance.PlayServiceDown(1);
                    }

                    _lastDownAudioAlert = DateTime.UtcNow;
                }
            }
        }

        private void TriggerUpAudio()
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                AudioMuteButton1.Opacity = 0.2;
            });
            if (Configuration?.AudioAlerts == true)
            {
                if (DateTime.UtcNow.Subtract(_lastUpAudioAlert) >= TimeSpan.FromSeconds(5))
                {
                    AudioService.Instance.PlayServiceUp();
                    _lastUpAudioAlert = DateTime.UtcNow;
                }
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            BuildPagesForScreenArea(new Size(Width, Height));
            base.OnOpened(e);
        }

        protected void HandleOnKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    {
                        // AudioService.Instance.PlayMessage();
                        break;
                    }
                case Key.Add:
                    // zoom in (make servers bigger)
                    if (!e.Handled) DoZoomIn();
                    break;
                case Key.OemPlus:
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && !e.Handled)
                    {
                        // zoom in (make servers bigger)
                        DoZoomIn();
                    }
                    break;
                case Key.Subtract:
                    // zoom out (make servers smaller)
                    if (!e.Handled) DoZoomOut();
                    break;
                case Key.OemMinus:
                    // zoom out (make servers smaller)
                    if (!e.Handled) DoZoomOut();
                    break;
                case Key.OemTilde:
                    {
                        // open history console
                        e.Handled = true;
                        if (ConsoleWindowOverlay.Height == 0)
                            ConsoleWindowOverlay.Show(Height);
                        else
                            ConsoleWindowOverlay.Collapse();
                        break;
                    }
            }
        }

        private void DoZoomIn()
        {
            var newValue = (int)MainWindow.Configuration!.Size + 1;
            if (newValue <= 3)
            {
                AudioService.Instance.PlayClick();
                MainWindow.Configuration!.Size = (UiSize)newValue;
                Debug.WriteLine($"Set View Size = {MainWindow.Configuration!.Size}");
                ServerPageViewModel.UpdateGlobalUiSize(MainWindow.Configuration!.Size);
                BuildPagesForScreenArea(ClientSize, forceUpdate: true);
                Toast.Info($"Zoom set to {(UiSize)newValue}", TimeSpan.FromSeconds(1));
            }
            else
            {
                AudioService.Instance.PlayCancel();
            }
        }

        private void DoZoomOut()
        {
            var newValue = (int)MainWindow.Configuration!.Size - 1;
            if (newValue >= 0)
            {
                AudioService.Instance.PlayClick();
                MainWindow.Configuration!.Size = (UiSize)newValue;
                Debug.WriteLine($"Set View Size = {MainWindow.Configuration!.Size}");
                ServerPageViewModel.UpdateGlobalUiSize(MainWindow.Configuration!.Size);
                BuildPagesForScreenArea(ClientSize, forceUpdate: true);
                Toast.Info($"Zoom set to {(UiSize)newValue}", TimeSpan.FromSeconds(1));
            }
            else
            {
                AudioService.Instance.PlayCancel();
            }

        }

        private IList<Server> CreateTestData()
        {
            Debug.WriteLine("CreateTestData");
            var servers = new List<Server>();
            var count = 24;
            for (var i = 0; i < count; i++)
            {
                servers.Add(new Server(DesignModeConstants.MonitorId, 1, $"Server Live {i}", null, i, true, new List<Models.ServiceState>() { new("HTTPS", true), new("SQL", false), new("DNS", true), new("ICMP", true) }));
            }
            servers.Add(new Server(DesignModeConstants.MonitorId, 1, $"Last Server", null, count, false, new List<Models.ServiceState>() { new("HTTPS", true), new("SQL", true), new("DNS", true) }));
            return servers;
        }
    }
}