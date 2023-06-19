using LightInject;
using NLog;
using NLog.Extensions.Logging;
using System.Reflection;
using SystemMonitor.Agent;
using SystemMonitor.Agent.Common;
using SystemMonitor.Common.IO.Security;

try
{
    ConsoleHelper.SetEncoding();
    var container = new LightInject.ServiceContainer();
    // Create the host for the service
    using var host = Host.CreateDefaultBuilder(args)
        .UseWindowsService((options) =>
        {
            options.ServiceName = "System Monitor Agent";
        })
        //.UseSystemd()
        .ConfigureServices(services =>
        {
            services.AddHostedService<AgentService>();
            services.AddSingleton<IServiceContainer>(container);
            services.AddSingleton<IAesEncryptionService, AesEncryptionService>();

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configurationRoot = builder.Build();
            LogManager.Configuration = new NLogLoggingConfiguration(configurationRoot.GetSection("NLog"));

            var logger = LogManager.GetCurrentClassLogger();
            // register all monitors
            var monitorsAssembly = Assembly.Load("SystemMonitor.Monitors");
            var monitorTypes = monitorsAssembly.GetTypes().Where(t => typeof(SystemMonitor.Common.Sdk.IMonitorAsync).IsAssignableFrom(t)).ToList();
            foreach (var monitorType in monitorTypes)
            {
                services.AddScoped(monitorType);
            }

            logger.Info($"Agent loaded {monitorTypes.Count} monitors.");
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger), typeof(Logger<object>));
            services.AddLogging((builder) =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddNLog();
            });

            var configuration = configurationRoot.GetRequiredSection(nameof(Configuration)).Get<Configuration>() ?? throw new Exception("Could not load configuration!");
            services.AddSingleton<Configuration>(configuration);
            ConsoleHelper.WriteEnvironmentInfo(configuration);
        })
        .Build();

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
