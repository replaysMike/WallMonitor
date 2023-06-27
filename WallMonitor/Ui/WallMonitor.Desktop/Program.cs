using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using System.Threading;

namespace WallMonitor.Desktop
{
    internal class Program
    {
        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();

        public static void Main(string[] args)
        {
            var builder = BuildAvaloniaApp();

            //builder.Start(AppMain, args);
            builder.StartWithClassicDesktopLifetime(args);
            (builder.Instance as App)!.Dispose();
        }

        static void AppMain(Application app, string[] args)
        {
            // A cancellation token source that will be used to stop the main loop
            var cts = new CancellationTokenSource();
            new Window().Show();

            // Start the main loop
            app.Run(cts.Token);
        }

    }
}