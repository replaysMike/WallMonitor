using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using LightInject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using SystemMonitor.Desktop.Controls;
using SystemMonitor.Desktop.Models;
using SystemMonitor.Desktop.Services;
using SystemMonitor.Desktop.ViewModels;

namespace SystemMonitor.Desktop.Views
{
    public partial class ConsoleWindow : UserControl
    {
        public ConsoleWindowModel ViewModel => (ConsoleWindowModel?)DataContext ?? new ConsoleWindowModel();
        private bool _allowScrollToEnd = true;
        private int _commandHistoryIndex = 0;
        private List<string> _commandHistory = new List<string>();

        public ConsoleWindow()
        {
            InitializeComponent();

            if (Design.IsDesignMode)
            {
                var context = new ConsoleWindowModel();
                context.AddHistory("Welcome to System Monitor v1.0", ConsoleLogLevel.None, false);
                context.AddHistory("Welcome to the history view.", ConsoleLogLevel.None, false);
                context.AddHistory("This is where you'll find a list of all items in the history.", ConsoleLogLevel.None, false);
                context.AddHistory("More things to view.", ConsoleLogLevel.None);
                DataContext = context;
            }

            ClearInput();
            AddHandler(InputElement.KeyDownEvent, Input1_KeyDown, Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
            ScrollWindow.ScrollChanged += ScrollWindow_ScrollChanged;
            History1.ContextFlyout = null;
            History1.ContextMenu = null;
        }

        private void ClearInput()
        {
            Input1.Text = ">";
            Input1.CaretIndex = 1;
        }

        private void ScrollWindow_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            //Debug.WriteLine($"Scroll changed ({e.ExtentDelta}) ({e.OffsetDelta}) ({e.ViewportDelta}) [{ScrollWindow.Offset}] {ScrollWindow.Viewport} {ScrollWindow.Extent}");
            if (ScrollWindow.Offset.Y < ScrollWindow.ScrollBarMaximum.Y)
            {
                // user has scrolled up the page, don't reset it on them
                _allowScrollToEnd = false;
            }
            else
            {
                _allowScrollToEnd = true;
            }
        }

        public void ScrollToEnd(bool force = false)
        {
            if (_allowScrollToEnd || force)
            {
                Dispatcher.UIThread.Invoke(() => ScrollWindow.ScrollToEnd());
            }
        }

        public void Show(double height)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Height = height - 25;
                IsHitTestVisible = true;
                Input1.Focus();
            });
        }

        public void Collapse()
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Height = 0;
                IsHitTestVisible = false;
            });
        }

        private void Input1_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                default:
                    break;
                case Avalonia.Input.Key.OemTilde:
                    e.Handled = true;
                    break;
                case Key.Back:
                    if (Input1.Text.Length == 0)
                    {
                        e.Handled = true;
                        ClearInput();
                    }
                    break;
                case Key.Up:
                    _commandHistoryIndex--;
                    if (_commandHistoryIndex < 0)
                        _commandHistoryIndex = 0;
                    Input1.Text = $">{_commandHistory[_commandHistoryIndex]}";
                    Input1.CaretIndex = Input1.Text.Length;
                    break;
                case Key.Down:
                    _commandHistoryIndex++;
                    if (_commandHistoryIndex > _commandHistory.Count)
                        _commandHistoryIndex = _commandHistory.Count;

                    if (_commandHistoryIndex >= _commandHistory.Count)
                        ClearInput();
                    else
                    {
                        Input1.Text = $">{_commandHistory[_commandHistoryIndex]}";
                        Input1.CaretIndex = Input1.Text.Length;
                    }

                    break;
                case Avalonia.Input.Key.Enter:
                    e.Handled = true;
                    var command = Input1.Text;
                    _commandHistory.Add(command.Substring(1));
                    _commandHistoryIndex = _commandHistory.Count;
                    RunCommand(command);
                    ClearInput();
                    break;
                case Key.C:
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    {
                        // copy
                        var clipboard = AvaloniaLocator.Current.GetRequiredService<IClipboard>();
                        if (!string.IsNullOrEmpty(History1.SelectedText))
                            clipboard.SetTextAsync(History1.SelectedText);
                        else if(!string.IsNullOrEmpty(Input1.SelectedText))
                            clipboard.SetTextAsync(Input1.SelectedText);
                    }
                    break;
            }
        }

        private void RunCommand(string command)
        {
            var parameters = command.Substring(1).Split(" ");
            if (parameters.Length == 0)
                return;
            switch (parameters[0].ToLower().Replace("/", ""))
            {
                default:
                case "help":
                    DisplayHelp();
                    break;
                case "clear":
                    ViewModel.Clear();
                    break;
                case "version":
                    try
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        var versionStr = assembly.GetName()?.Version?.ToString() ?? "0.0.0.0";
                        ViewModel.AddHistory($"Version {versionStr}", ConsoleLogLevel.None);
                    }
                    catch (Exception)
                    {
                        ViewModel.AddHistory("Failed to get current version information.", ConsoleLogLevel.None);
                    }
                    break;
                case "audio":
                    if (parameters.Length > 1)
                    {
                        if (parameters[1].ToLower() == "on")
                        {
                            AudioService.Instance.IsMuted = false;
                            ViewModel.AddHistory("Audio enabled.", ConsoleLogLevel.None);
                        }
                        else if (parameters[1].ToLower() == "off")
                        {
                            AudioService.Instance.IsMuted = true;
                            ViewModel.AddHistory("Audio silenced.", ConsoleLogLevel.None);
                        }
                        else
                        {
                            ViewModel.AddHistory($"Unknown parameter '{parameters[1]}'", ConsoleLogLevel.None);
                        }
                    }
                    else
                    {
                        ViewModel.AddHistory("Incorrect number of parameters.", ConsoleLogLevel.None);
                    }
                    break;
                case "loglevel":
                    if (parameters.Length > 1)
                    {
                        if (parameters[1].ToLower() == "verbose")
                        {
                            ViewModel.LogLevel = ConsoleLogLevel.Verbose;
                            ViewModel.AddHistory("Console logging set to verbose.", ConsoleLogLevel.None);
                        }
                        else if (parameters[1].ToLower() == "normal")
                        {
                            ViewModel.LogLevel = ConsoleLogLevel.Normal;
                            ViewModel.AddHistory("Console logging set to normal verbosity.", ConsoleLogLevel.None);
                        }
                        else if (parameters[1].ToLower() == "none")
                        {
                            ViewModel.LogLevel = ConsoleLogLevel.None;
                            ViewModel.AddHistory("All console logging off.", ConsoleLogLevel.None);
                        }
                        else
                        {
                            ViewModel.AddHistory($"Unknown parameter '{parameters[1]}'", ConsoleLogLevel.None);
                        }
                    }
                    else
                    {
                        ViewModel.AddHistory("Incorrect number of parameters.", ConsoleLogLevel.None);
                    }
                    break;
                case "cycle":
                    if (parameters.Length > 1)
                    {
                        var mainWindow = App.Container.GetInstance<MainWindow>();
                        if (parameters[1].ToLower() == "on")
                        {
                            mainWindow.EnableCyclePages();
                            ViewModel.AddHistory("Page cycling enabled.", ConsoleLogLevel.None);
                        }
                        else if (parameters[1].ToLower() == "off")
                        {
                            mainWindow.DisableCyclePages();
                            ViewModel.AddHistory("Page cycling disabled.", ConsoleLogLevel.None);
                        }
                        if (parameters[1].Contains(":") || parameters.Length > 2 && parameters[2].Contains(":"))
                        {
                            var intervalStr = parameters[1].Contains(":") ? parameters[1] : parameters[2];
                            if (TimeSpan.TryParse(intervalStr, out var interval))
                            {

                                mainWindow.SetCyclePagesInterval(interval);
                                ViewModel.AddHistory($"Page cycling interval set to {interval}.", ConsoleLogLevel.None);
                            }
                            else
                            {
                                ViewModel.AddHistory("Please specify interval in the format of 00:00:00 (hours:minutes:seconds)", ConsoleLogLevel.None);
                            }
                        }
                    }
                    else
                    {
                        ViewModel.AddHistory("Incorrect number of parameters.", ConsoleLogLevel.None);
                    }
                    break;
                case "list":
                    if (parameters.Length > 1)
                    {
                        if (parameters[1].ToLower() == "down")
                        {
                            var mainWindow = App.Container.GetInstance<MainWindow>();
                            var count = mainWindow._allServers.Count(x => x.CurrentState == Models.State.ServerDown || x.CurrentState == Models.State.ParentMonitoringServiceIsDown);
                            ViewModel.AddHistory($"There are {count} servers in the down state.", ConsoleLogLevel.None);
                            foreach (var server in mainWindow._allServers)
                            {
                                if (server.CurrentState == Models.State.ServerDown || server.CurrentState == Models.State.ParentMonitoringServiceIsDown)
                                {
                                    ViewModel.AddHistory($"{server.Name} ({server.HostName}) - {server.CurrentState}", ConsoleLogLevel.None);
                                }
                            }
                            ViewModel.AddHistory($"End of list.", ConsoleLogLevel.None);
                        }
                        else if (parameters[1].ToLower() == "up")
                        {
                            var mainWindow = App.Container.GetInstance<MainWindow>();
                            var count = mainWindow._allServers.Count(x => x.CurrentState == Models.State.ServerIsRunning);
                            ViewModel.AddHistory($"There are {count} servers in the up state.", ConsoleLogLevel.None);
                            foreach (var server in mainWindow._allServers)
                            {
                                if (server.CurrentState == Models.State.ServerIsRunning)
                                {
                                    ViewModel.AddHistory($"{server.Name} ({server.HostName}) - {server.CurrentState}", ConsoleLogLevel.None);
                                }
                            }
                            ViewModel.AddHistory($"End of list.", ConsoleLogLevel.None);
                        }
                        else if (parameters[1].ToLower() == "disabled")
                        {
                            var mainWindow = App.Container.GetInstance<MainWindow>();
                            var count = mainWindow._allServers.Count(x => x.CurrentState == Models.State.ServerIsNotRunning && !x.IsEnabled);
                            ViewModel.AddHistory($"There are {count} servers in the disabled state.", ConsoleLogLevel.None);
                            foreach (var server in mainWindow._allServers)
                            {
                                if (server.CurrentState == Models.State.ServerIsNotRunning && !server.IsEnabled)
                                {
                                    ViewModel.AddHistory($"{server.Name} ({server.HostName}) - {server.CurrentState}", ConsoleLogLevel.None);
                                }
                            }
                            ViewModel.AddHistory($"End of list.", ConsoleLogLevel.None);
                        }
                        else
                        {
                            ViewModel.AddHistory($"Unknown parameter '{parameters[1]}'", ConsoleLogLevel.None);
                        }
                    }
                    else
                    {
                        ViewModel.AddHistory("Incorrect number of parameters.", ConsoleLogLevel.None);
                    }
                    break;
                case "ip":
                    var ipAddresses = Dns.GetHostAddresses(Dns.GetHostName());
                    ViewModel.AddHistory($"There are {ipAddresses.Length} ip addresses bound.", ConsoleLogLevel.None);
                    foreach (var ip in ipAddresses)
                    {
                        var type = ip.AddressFamily.ToString().Replace("InterNetworkV6", "IPV6").Replace("InterNetwork", "IPV4");
                        ViewModel.AddHistory($"- {ip} ({type})", ConsoleLogLevel.None);
                    }
                    
                    // get external IP
                    try
                    {
                        var httpClient = new HttpClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(3);
                        var externalIpString = (httpClient.GetStringAsync("http://icanhazip.com")).GetAwaiter().GetResult()
                            .Replace("\\r\\n", "").Replace("\\n", "").Trim();
                        ViewModel.AddHistory($"External IP is {externalIpString}", ConsoleLogLevel.None);
                    }
                    catch (Exception)
                    {
                        ViewModel.AddHistory($"Unable to determine external IP address.", ConsoleLogLevel.None);
                    }

                    ViewModel.AddHistory($"End of list.", ConsoleLogLevel.None);
                    break;
                case "pause":
                case "resume":
                    ViewModel.AddHistory("Currently not supported.", ConsoleLogLevel.None);
                    break;
                case "toast":
                    if (parameters.Length == 2)
                    {
                        Toast.Info(parameters[1]);
                    }
                    else if (parameters.Length > 2)
                    {
                        switch (parameters[1].ToLower())
                        {
                            case "info":
                                Toast.Info(string.Join(" ", parameters.Skip(2)));
                                break;
                            case "success":
                                Toast.Success(string.Join(" ", parameters.Skip(2)));
                                break;
                            case "warn":
                                Toast.Warning(string.Join(" ", parameters.Skip(2)));
                                break;
                            case "error":
                                Toast.Error(string.Join(" ", parameters.Skip(2)));
                                break;
                            default:
                                Toast.Info(string.Join(" ", parameters.Skip(1)));
                                break;
                        }
                    }
                    else
                    {
                        ViewModel.AddHistory("Incorrect number of parameters.", ConsoleLogLevel.None);
                    }
                    break;
                case ":wq":
                case ":q!":
                    Collapse();
                    break;
                case "quit":
                    ViewModel.AddHistory("Quitting!", ConsoleLogLevel.None);
                    Thread.Sleep(500);
                    (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
                    break;
            }
        }

        private void DisplayHelp()
        {
            ViewModel.AddHistory(@"
╭────────────────────────╮
│  System Monitor Help   │
╰────────────────────────╯

/help                           Display help options
/pause                          Pause all checking of services
/resume                         Resume all checking of services
/version                        Display version
/ip                             Display IP Address information
/audio [on/off]                 Toggle all sounds on or off
/list [down/up/disabled]        List all servers marked down/up
/loglevel [none/normal/verbose] Set the console log level
/cycle [on/off] [00:00:00]      Toggle cycling between pages
/clear                          Clear the log
/quit                           Quit
", ConsoleLogLevel.None, false);
            ScrollToEnd();
        }
    }
}
