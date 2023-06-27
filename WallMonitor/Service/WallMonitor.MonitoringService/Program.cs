using FluentEmail.Core;
using LightInject;
using NLog;
using NLog.Extensions.Logging;
using System.Reflection;
using FluentEmail.Core.Interfaces;
using WallMonitor.Common.Notifications;
using WallMonitor.Licensing;
using WallMonitor.MonitoringService;
using FluentEmail.Smtp;
using Microsoft.Extensions.DependencyInjection;

try
{
    var container = new LightInject.ServiceContainer();
    // Create the host for the service
    using var host = Host.CreateDefaultBuilder(args)
        .UseWindowsService((options) =>
        {
            options.ServiceName = "System Monitor";
        })
        //.UseSystemd()
        .ConfigureServices(services =>
        {
            services.AddHostedService<MonitoringService>();

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configurationRoot = builder.Build();
            LogManager.Configuration = new NLogLoggingConfiguration(configurationRoot.GetSection("NLog"));
            var logger = LogManager.GetCurrentClassLogger();

            var configuration = configurationRoot.GetRequiredSection(nameof(Configuration)).Get<Configuration>();

            var monitorsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Monitors");
            var existingConfigurations = configuration.Services.Count;
            logger.Info($"Scanning folder '{monitorsPath}' for monitor configurations...");
            configuration = ConfigurationScanner.ScanFolder(monitorsPath, configuration, logger);
            logger.Info($"Added {configuration.Services.Count - existingConfigurations} hosts configurations.");

            services.AddSingleton<IServiceContainer>(container);

            // register all monitors
            try
            {
                var monitorsAssembly = Assembly.Load("WallMonitor.Monitors");
                var monitorTypes = monitorsAssembly.GetTypes().Where(t => typeof(WallMonitor.Common.Sdk.IMonitorAsync).IsAssignableFrom(t)).ToList();
                foreach (var monitorType in monitorTypes)
                {
                    services.AddScoped(monitorType);
                }

                logger.Info($"Loaded {monitorTypes.Count} IMonitorAsync implementations in assembly 'WallMonitor.Monitors'.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load IMonitorAsync implementations from assembly 'WallMonitor.Monitors'");
            }

            services.AddSingleton<IConfiguration>(configurationRoot);
            services.AddSingleton<Configuration>(configuration);
            services.AddSingleton<NotificationConfiguration>(configuration.Notifications);
            services.AddSingleton<EmailConfiguration>(configuration.Notifications.Email);
            services.AddSingleton<SmsConfiguration>(configuration.Notifications.Sms);
            services.AddSingleton<SnmpConfiguration>(configuration.Notifications.Snmp);

            // register notification services
            services.AddTransient<FluentEmailAwsSesRawSender>();
            services.AddTransient<EmailTemplateManager>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<ISmsService, SmsService>();
            services.AddTransient<ISnmpService, SnmpService>();
            services.AddTransient<INotificationService, NotificationService>();

            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger), typeof(Logger<object>));
            services.AddLogging((builder) =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddNLog();
            });
            logger.Info($"Preparing to monitor {configuration.Services.Count} hosts...");

            // configure email services
            var emailBuilder = services.AddFluentEmail(configuration.Notifications.Email.FromEmailAddress, configuration.Notifications.Email.FromEmailName)
                .AddRazorRenderer();
            // configure email by configuration provider
            switch (configuration.Notifications.Email.EmailProvider)
            {
                case EmailProviders.Smtp:
                    /*emailBuilder.AddSmtpSender(() => new System.Net.Mail.SmtpClient
                    {
                        Timeout = (int)configuration.Notifications.Email.Timeout.TotalMilliseconds,
                        Host = configuration.Notifications.Email.SmtpServer,
                        Port = configuration.Notifications.Email.Port
                    });*/
                    emailBuilder.Services.AddSingleton<ISender>((provider) =>
                    {
                        var client = new System.Net.Mail.SmtpClient
                        {
                            Timeout = (int)configuration.Notifications.Email.Timeout.TotalMilliseconds,
                            Host = configuration.Notifications.Email.SmtpServer,
                            Port = configuration.Notifications.Email.Port,
                        };
                        if (!string.IsNullOrEmpty(configuration.Notifications.Email.SmtpUsername))
                        {
                            // use smtp authentication
                            client.Credentials = new System.Net.NetworkCredential(configuration.Notifications.Email.SmtpUsername, configuration.Notifications.Email.SmtpPassword);
                        }
                        return new SmtpSender(client);
                    });
                    break;
                case EmailProviders.AwsSes:
                    emailBuilder.Services.Add(ServiceDescriptor.Scoped(serviceProvider => (ISender)serviceProvider.GetRequiredService<IEmailService>()));
                    break;
            }
        })
        .Build();

    // Run the service
    await host.RunAsync();
}
catch (Exception ex)
{
    var logger = LogManager.GetCurrentClassLogger();
    logger.Error(ex, "A global exception occurred.");
}
finally
{
    LogManager.Shutdown();
}