using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using WallMonitor.Common;
using WallMonitor.Desktop.Models;

namespace WallMonitor.Desktop.ViewModels
{
    public class CheckItemEntry
    {
        public string ServerName { get; init; }
        public string ServiceName { get; init; }
        public DateTime StartedChecking { get; init; }
        public bool IsReadyToRemove { get; set; }

        public CheckItemEntry(string serverName, string serviceName, DateTime startedChecking, bool isReadyToRemove)
        {
            ServerName = serverName;
            ServiceName = serviceName;
            StartedChecking = startedChecking;
            IsReadyToRemove = isReadyToRemove;
        }
    }

    public class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// Set the minimum display time of the IsChecking color highlighting.
        /// Without this, it may only appear for a few milliseconds (hence the complicated queue/timer behavior in the model itself)
        /// </summary>
        private static readonly TimeSpan MinDisplayTime = TimeSpan.FromMilliseconds(800);

        public static readonly TimeSpan AutoResetAllowAutoPageNavigationAfter = TimeSpan.FromSeconds(60);
        private static readonly StringComparison StringComparison = StringComparison.InvariantCultureIgnoreCase;
        private readonly object _queueLock = new();
        private Guid _id = Guid.NewGuid();
        private readonly List<CheckItemEntry> _checkItemQueue = new();
        private readonly DispatcherTimer _checkItemQueueTimer = new();

        public int PageCount => Pages.Count;

        private List<ServerPageViewModel> _pages = new();
        public List<ServerPageViewModel> Pages
        {
            get => _pages;
            set => this.RaiseAndSetIfChanged(ref _pages, value);
        }
        //public ObservableCollection<ServerPageViewModel> Pages { get; set; } = new();

        private int _currentPage;
        public int CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public int TotalServers { get; set; }
        public int ServersPerRow { get; set; }
        public int ServersPerColumn { get; set; }
        public bool AllowAutoPageNavigation { get; set; } = true;
        public DateTime LastUserClickedNaviation { get; set; }

        /// <summary>
        /// User clicked a pagination navigation button
        /// </summary>
        public void UserClickedNavigation()
        {
            if (Pages.Any(x => x.HasFailures))
            {
                AllowAutoPageNavigation = false;
                LastUserClickedNaviation = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Update the state for an entire server
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="state"></param>
        public void UpdateServer(string serverName, State state)
        {
            foreach (var page in Pages)
            {
                var match = page.Servers.FirstOrDefault(x => x.Name.Equals(serverName, StringComparison));
                if (match != null)
                {
                    match.CurrentState = state;
                    var services = match.Services.ToList();
                    // update the current state for all services
                    foreach (var service in services)
                    {
                        service.CurrentState = state;
                        service.IsDown = state == State.ServerDown;
                    }
                }
                page.HasFailures = page.Servers.Any(x => x.CurrentState == State.ServerDown || x.CurrentState == State.ParentMonitoringServiceIsDown);
            }
            ResetAllowAutoPageNaviation();
        }

        /// <summary>
        /// Reset allowing of auto page navigation on failure
        /// </summary>
        private void ResetAllowAutoPageNaviation()
        {
            if (!Pages.Any(x => x.HasFailures))
            {
                AllowAutoPageNavigation = true;
            }
        }

        public void UpdateServer(string serverName, string serviceName, bool isChecking, double? value, string? range, Units units, TimeSpan responseTime, GraphType graphType)
            => UpdateServer(serverName, serviceName, null, isChecking, value, range, units, responseTime, graphType);

        public void UpdateServer(string serverName, string serviceName, State state, double? value, string? range, Units units, TimeSpan responseTime, GraphType graphType)
            => UpdateServer(serverName, serviceName, state, null, value, range, units, responseTime, graphType);

        /// <summary>
        /// Update a specific service for a server
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="serviceName"></param>
        /// <param name="state"></param>
        /// <param name="isChecking"></param>
        /// <param name="value"></param>
        /// <param name="range"></param>
        /// <param name="units"></param>
        /// <param name="responseTime"></param>
        /// <param name="graphType"></param>
        public void UpdateServer(string serverName, string serviceName, State? state, bool? isChecking, double? value, string? range, Units units, TimeSpan responseTime, GraphType graphType)
        {
            //Debug.WriteLine($"UpdateServer: '{serviceName}' state:{state} value:{value} range:{range}");

            foreach (var page in Pages)
            {
                var match = page.Servers.FirstOrDefault(x => x.Name.Equals(serverName, StringComparison));
                if (match != null)
                {
                    if (state != null)
                    {
                        // update the current state for the entire server
                        match.CurrentState = state.Value;
                        var services = match.Services.Where(x => x.Name.Equals(serviceName, StringComparison) || serviceName == "*").ToList();
                        if (services.Any())
                        {
                            foreach (var service in services)
                            {
                                // update the current state for a specific service
                                service.CurrentState = state.Value;
                                service.Value = value;
                                service.GraphType = graphType;
                                service.Range = range;
                                service.Units = units;
                                service.ResponseTime = responseTime;
                                service.IsDown = state.Value == State.ServerDown;
                            }
                        }
                    }

                    if (isChecking != null)
                    {
                        // update the isChecking for the entire server
                        var allowIsCheckingUpdate = false;
                        if (isChecking.Value)
                        {
                            lock (_queueLock)
                            {
                                _checkItemQueue.Add(new CheckItemEntry(serverName, serviceName, DateTime.UtcNow, false));
                            }
                            allowIsCheckingUpdate = true;
                        }
                        else
                        {
                            lock (_queueLock)
                            {
                                var queuedItem = _checkItemQueue.FirstOrDefault(x => x.ServerName == serverName && x.ServiceName == serviceName);
                                if (queuedItem != null)
                                {
                                    if (queuedItem.IsReadyToRemove)
                                    {
                                        // timer has met minimum show requirement, dequeue and update immediately
                                        allowIsCheckingUpdate = true;
                                        _checkItemQueue.Remove(queuedItem);
                                    }
                                    else
                                    {
                                        // is queued for later update
                                        queuedItem.IsReadyToRemove = true;
                                    }
                                }
                                else
                                {
                                    // it can be updated directly
                                    allowIsCheckingUpdate = true;
                                }
                            }
                        }
                        if (allowIsCheckingUpdate)
                            match.IsChecking = isChecking.Value;
                        var services = match.Services.Where(x => x.Name.Equals(serviceName, StringComparison) || serviceName == "*").ToList();
                        if (services.Any())
                        {
                            foreach (var service in services)
                            {
                                // update the isChecking for a specific service
                                if (allowIsCheckingUpdate)
                                    service.IsChecking = isChecking.Value;
                                service.Value = value;
                                service.GraphType = graphType;
                                service.Range = range;
                                service.Units = units;
                                service.ResponseTime = responseTime;
                            }
                        }
                    }

                    break;
                }
            }

            ResetAllowAutoPageNaviation();
        }

        public MainWindowViewModel()
        {
            // for design mode
            if (Design.IsDesignMode)
            {
                Pages.Add(new ServerPageViewModel());
                Pages.Add(new ServerPageViewModel());
                Pages.Add(new ServerPageViewModel());
            }

            _checkItemQueueTimer.Interval = TimeSpan.FromMilliseconds(100);
            _checkItemQueueTimer.Tick += _checkItemQueueTimer_Tick;
            _checkItemQueueTimer.Start();
        }

        private void _checkItemQueueTimer_Tick(object? sender, EventArgs e)
        {
            if (_checkItemQueue.Count > 0)
            {
                var itemsToRemove = new List<CheckItemEntry>();
                lock (_queueLock)
                {
                    foreach (var item in _checkItemQueue)
                    {
                        if (DateTime.UtcNow.Subtract(item.StartedChecking) >= MinDisplayTime)
                        {
                            if (item.IsReadyToRemove)
                            {
                                itemsToRemove.Add(item);

                                // need to update the server/service pair
                                foreach (var page in Pages)
                                {
                                    var match = page.Servers.FirstOrDefault(x => x.Name.Equals(item.ServerName, StringComparison));
                                    if (match != null)
                                    {
                                        match.IsChecking = false;
                                        var services = match.Services.Where(x => x.Name.Equals(item.ServiceName, StringComparison) || item.ServiceName == "*").ToList();
                                        if (services.Any())
                                        {
                                            foreach (var service in services)
                                            {
                                                service.IsChecking = false;
                                            }
                                        }

                                        break;
                                    }
                                }
                            }
                            else
                            {
                                item.IsReadyToRemove = true;
                            }
                        }
                    }
                    _checkItemQueue.RemoveMany(itemsToRemove);
                }
            }

            // perform some maintenance tasks
            if (!AllowAutoPageNavigation && DateTime.UtcNow.Subtract(LastUserClickedNaviation) >= AutoResetAllowAutoPageNavigationAfter)
            {
                // re-enable auto page navigation based on time
                AllowAutoPageNavigation = true;
            }
        }
    }
}