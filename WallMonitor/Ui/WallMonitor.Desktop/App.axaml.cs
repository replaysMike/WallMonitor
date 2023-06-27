using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using LightInject;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WallMonitor.Common;
using WallMonitor.Common.IO;
using WallMonitor.Common.IO.Security;
using WallMonitor.Desktop.Models;
using WallMonitor.Desktop.Services;
using WallMonitor.Desktop.Views;

namespace WallMonitor.Desktop
{
    public partial class App : Application, IDisposable
    {
        private readonly IMessageNotificationService _messageNotificationService;
        private readonly List<IListener> _monitoringServices = new ();
        public static IServiceContainer Container { get; }

        static App()
        {
            Container = new LightInject.ServiceContainer();
            Container.RegisterSingleton<MainWindow>();
            Container.RegisterSingleton<IAesEncryptionService, AesEncryptionService>();
            Container.RegisterInstance<IMessageNotificationService>(new MessageNotificationService());

            Container.RegisterScoped<ILoggerFactory, LoggerFactory>();
            Container.RegisterScoped(typeof(ILogger<>), typeof(Logger<>));
            Container.RegisterScoped(typeof(ILogger), typeof(Logger<object>));
        }

        public App()
        {
            RequestedThemeVariant = ThemeVariant.Dark;
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configurationRoot = builder.Build();
            var configuration = configurationRoot.GetRequiredSection("Configuration").Get<MonitoringServicesConfiguration>() ?? throw new InvalidOperationException("Missing configuration section named 'Configuration'");
            Container.RegisterSingleton<MonitoringServicesConfiguration>((factory) => configuration);

            Container.BeginScope();
            
            _messageNotificationService = Container.GetInstance<IMessageNotificationService>();

            if (!Design.IsDesignMode)
            {
                foreach (var monitorService in configuration.MonitorHosts)
                {
                    IListener? service = null;
                    var endpoint = new Uri(monitorService.Endpoint);
                    switch (endpoint.Scheme.ToLower())
                    {
                        case "udp":
                            service = new UdpListener(monitorService.Name, endpoint, monitorService.OrderId, Container.GetInstance<ILogger>(), monitorService.EncryptionKey, Container.GetInstance<IAesEncryptionService>());
                            break;
                        case "tcp":
                            service = new TcpListener(monitorService.Name, endpoint, monitorService.OrderId, Container.GetInstance<ILogger>(), monitorService.EncryptionKey, Container.GetInstance<IAesEncryptionService>());
                            break;
                        default:
                            throw new NotSupportedException($"Monitoring service named '{monitorService.Name}' specifies endpoint '{endpoint}' is not supported. Must specify either udp or tcp.");
                    }
                    service.ConfigurationEventReceived += Service_ConfigurationEventReceived;
                    service.ServerEventReceived += Service_ServerEventReceived;
                    service.ConnectionLost += Service_ConnectionLost;
                    service.ConnectionRestored += Service_ConnectionRestored;
                    _monitoringServices.Add(service);
                    service.Start();
                }
            }

            AudioService.Instance.EnsureCreated();
        }

        private void Service_ConnectionRestored(object? sender, ConnectionRestoredEventArgs e)
        {
            var listener = sender as IListener;
            if (listener == null) return;

            Debug.WriteLine($"Received connection restored on '{listener.Id}' to {e.Name} ({e.Uri})");
            //Toast.Error($"Connection lost to {e.Name} ({e.Uri})");
            _messageNotificationService.SendServerMessage(new ServerMessage(listener.Id, EventType.ServiceRestored, $"Monitoring service '{e.Name}' is back online."));
        }

        private void Service_ConnectionLost(object? sender, ConnectionLostEventArgs e)
        {
            var listener = sender as IListener;
            if (listener == null) return;

            Debug.WriteLine($"Received connection lost on '{listener.Id}' to {e.Name} ({e.Uri})");
            //Toast.Error($"Connection lost to {e.Name} ({e.Uri})");
            _messageNotificationService.SendServerMessage(new ServerMessage(listener.Id, EventType.ServiceOffline, $"Monitoring service '{e.Name}' is offline."));
        }

        private void Service_ServerEventReceived(object? sender, ServerNotificationEventArgs e)
        {
            var listener = sender as IListener;
            if (listener == null) return;
            // Debug.WriteLine($"Received {e.EventType}");
            _messageNotificationService.SendMonitoringServiceMessage(new ServiceUpdateMessage(listener.Id, e.EventType, e.Host, e.Service, e.DateTime, e.ServiceState, e.Value, e.Range, e.Units, TimeSpan.FromTicks(e.ResponseTime), e.LastUpTime, e.PreviousDownTime, e.GraphType));
        }

        private void Service_ConfigurationEventReceived(object? sender, MonitorConfigurationEventArgs e)
        {
            var listener = sender as IListener;
            Debug.WriteLine($"Received configuration from {e.Configuration.Monitor}");
            if (listener == null) return;
            
            // configure display for servers
            Dispatcher.UIThread.Invoke(() =>
            {
                var mainWindow = Container.GetInstance<MainWindow>();
                try
                {
                    mainWindow.UpdateServerConfiguration(listener.Id, listener.OrderId, e.Configuration);
                }
                catch (TaskCanceledException)
                {
                }
            });
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = Container.GetInstance<MainWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void Dispose()
        {
            AudioService.Instance.Dispose();
            foreach (var service in _monitoringServices)
            {
                service.Dispose();
            }
            Container.Dispose();
        }
    }
}