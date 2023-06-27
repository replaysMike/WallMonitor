#if OS_WINDOWS
using System.Diagnostics;

namespace WallMonitor.Agent.Windows
{
    public class PerformanceCounters : IDisposable
    {
        private readonly IDictionary<string, PerformanceCounter> _counters = new Dictionary<string, PerformanceCounter>();

        public PerformanceCounters()
        {
            ListCounters("Processor Information");
        }

        public void ListCounters(string categoryName)
        {
            PerformanceCounterCategory category = PerformanceCounterCategory.GetCategories().First(c => c.CategoryName == categoryName);
            Console.WriteLine("{0} [{1}]", category.CategoryName, category.CategoryType);

            string[] instanceNames = category.GetInstanceNames();

            if (instanceNames.Length > 0)
            {
                // MultiInstance categories
                foreach (string instanceName in instanceNames)
                {
                    ListInstances(category, instanceName);
                }
            }
            else
            {
                // SingleInstance categories
                ListInstances(category, string.Empty);
            }

            Console.WriteLine("End of Counters");
        }

        private static void ListInstances(PerformanceCounterCategory category, string instanceName)
        {
            Console.WriteLine("  Instance:  {0}", instanceName);
            PerformanceCounter[] counters = category.GetCounters(instanceName);

            foreach (PerformanceCounter counter in counters)
            {
                Console.WriteLine("    {0}", counter.CounterName);
            }
            Console.WriteLine("  End of Instance:  {0}", instanceName);
        }
        
        /// <summary>
        /// Create a cpu usage performance counter
        /// </summary>
        /// <param name="name"></param>
        public void CreateCpuUsage(string name)
        {
            if (!_counters.ContainsKey(name))
            {
                // "_Total" is not available in a Docker container
                var pc = new PerformanceCounter("Processor Information", "% Processor Time");
                _counters.Add(name, pc);
            }
        }

        /// <summary>
        /// Create a memory usage performance counter
        /// </summary>
        /// <param name="name"></param>
        public void CreateMemoryAvailable(string name)
        {
            if (!_counters.ContainsKey(name))
            {
                var pc = new PerformanceCounter("Memory", "Available MBytes");
                _counters.Add(name, pc);
            }
        }

        /// <summary>
        /// Get the next value from the performance counter
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public double GetNextValue(string key)
        {
            return _counters[key].NextValue();
        }

        public void Dispose()
        {
            foreach (var counter in _counters)
            {
                counter.Value.Dispose();
            }
        }
    }
}
#endif
