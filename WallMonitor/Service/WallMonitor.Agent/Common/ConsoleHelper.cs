using System.Runtime.InteropServices;
using System.Text;

namespace WallMonitor.Agent.Common
{
    public static class ConsoleHelper
    {
        public static void SetEncoding()
        {
            if (!Console.IsOutputRedirected)
                Console.OutputEncoding = Encoding.UTF8;
        }

        public static void WriteEnvironmentInfo(Configuration configuration)
        {
            if (Console.IsOutputRedirected)
                return;

            // helps with docker logs
            Console.WriteLine();

            // write header
            WriteHeader("WallMonitor.Agent");

            // write environment info
            WriteDataLine("In Docker", EnvironmentUtils.InDocker);
            WriteDataLine("OS Platform", Environment.OSVersion.Platform);
            WriteDataLine("OS Version", Environment.OSVersion.VersionString);
            WriteDataLine("MachineName", Environment.MachineName);
            WriteDataLine("64Bit Process", Environment.Is64BitProcess);
            WriteDataLine("64Bit OS", Environment.Is64BitOperatingSystem);
            WriteDataLine("Processor Count", Environment.ProcessorCount);
            WriteDataLine("Command Line", Environment.CommandLine);

            WriteDataLine("Is Windows", RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            WriteDataLine("Is Linux", RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            WriteDataLine("Is OSX", RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
#if OS_WINDOWS
            WriteDataLine("OS is", "Windows");
#endif
#if OS_LINUX
            WriteDataLine("OS is", "Linux");
#endif
            WriteDataLine("IP Address", configuration.Ip);
            WriteDataLine("Port", configuration.Port);
            WriteDataLine("Modules", string.Join(",", configuration.Modules));
            WriteDataLine("Accept", string.Join(",", configuration.AllowFrom));
        }

        public static void WriteLine(string line, ConsoleColor textColor = ConsoleColor.Gray)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = textColor;
            Console.WriteLine(line);
            Console.ForegroundColor = originalColor;
        }

        public static void WriteDataLine(string line, object value, ConsoleColor lineColor = ConsoleColor.Gray, ConsoleColor valueColor = ConsoleColor.Cyan)
        {
            if (Console.IsOutputRedirected)
                return;
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = lineColor;
            Console.Write($"{line}: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[");
            if (value?.GetType() == typeof(bool))
            {
                if ((bool)value)
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ForegroundColor = ConsoleColor.Red;
            }
            else
            {
                Console.ForegroundColor = valueColor;
            }
            Console.Write($"{value}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");
            Console.ForegroundColor = originalColor;
        }

        public static void WriteHeader(string line, ConsoleColor boxColor = ConsoleColor.Green, ConsoleColor textColor = ConsoleColor.White)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = boxColor;
            Console.Write("╭");
            for (var i = 0; i < line.Length + 4; i++)
                Console.Write("─");
            Console.WriteLine("╮");
            Console.Write("│  ");
            Console.ForegroundColor = textColor;
            Console.Write(line);
            Console.ForegroundColor = boxColor;
            Console.WriteLine("  │");
            Console.Write("╰");
            for (var i = 0; i < line.Length + 4; i++)
                Console.Write("─");
            Console.WriteLine("╯");
            Console.ForegroundColor = originalColor;
        }
    }
}
