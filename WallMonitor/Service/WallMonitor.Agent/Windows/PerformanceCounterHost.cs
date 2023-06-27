#if OS_WINDOWS
using System.Diagnostics;
using System.Runtime.InteropServices;
using WallMonitor.Agent.Common;
#endif

namespace WallMonitor.Agent.Windows
{
    /// <summary>
    /// Exposes custom performance counters to monitor (Windows only)
    /// </summary>
    public class PerformanceCounterHost : IDisposable
    {
#if OS_WINDOWS
        public string CounterName { get; set; } = string.Empty;
        private readonly PerformanceCounter? _performanceCounter;
        private const string CategoryName = "SystemMonitor";
        private const string CategoryHelp = "SystemMonitor real time statistics";

        private static readonly List<CounterCreationData> Counters = new List<CounterCreationData>
        {
            new ("CPU Processor Time %", "Displays CPU processor time %", PerformanceCounterType.RateOfCountsPerSecond64),
            new ("Memory Available", "Displays Memory available in bytes", PerformanceCounterType.RateOfCountsPerSecond64),
        };

        static PerformanceCounterHost()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !EnvironmentUtils.InDocker)
            {
                //PerformanceCounterCategory.Delete(CategoryName);
                var customCategory = new PerformanceCounterCategory(CategoryName);
                var categoryExists = PerformanceCounterCategory.Exists(CategoryName);
                var allCountersExist = false;
                if (categoryExists)
                {
                    foreach (CounterCreationData counter in Counters)
                    {
                        if (!customCategory.CounterExists(counter.CounterName))
                        {
                            allCountersExist = false;
                            break;
                        }

                        allCountersExist = true;
                    }
                }

                if (!categoryExists || !allCountersExist)
                {
                    if (categoryExists || allCountersExist)
                        PerformanceCounterCategory.Delete(CategoryName);
                    PerformanceCounterCategory.Create(CategoryName, CategoryHelp, PerformanceCounterCategoryType.SingleInstance,
                        new CounterCreationDataCollection(Counters.ToArray()));
                    Console.WriteLine($"Created performance counters in category {CategoryName}");
                }
            }
        }

#endif
        public PerformanceCounterHost(string counterName)
        {
#if OS_WINDOWS
            if (!EnvironmentUtils.InDocker)
            {
                CounterName = counterName;
                Console.WriteLine($"Opening performance counter {counterName}");
                if (!Counters.Any(x => x.CounterName.Equals(counterName)))
                    throw new ArgumentException($"Unknown counter name '{counterName}'");
                _performanceCounter = new PerformanceCounter(CategoryName, counterName, false);
            }
#endif
        }

        /// <summary>
        /// Report data to the performance counter
        /// </summary>
        /// <param name="value"></param>
        public void ReportData(double value)
        {
#if OS_WINDOWS
            if (_performanceCounter != null)
            {
                _performanceCounter.RawValue = (long)value;
                _performanceCounter.Increment();
            }
#endif
        }

        /// <summary>
        /// Report data to the performance counter
        /// </summary>
        /// <param name="value"></param>
        public void ReportData(long value)
        {
#if OS_WINDOWS
            if (_performanceCounter != null)
            {
                _performanceCounter.RawValue = value;
                _performanceCounter.Increment();
            }
#endif
        }

        public void Dispose()
        {
#if OS_WINDOWS
            _performanceCounter?.Dispose();
#endif
        }
    }
}
