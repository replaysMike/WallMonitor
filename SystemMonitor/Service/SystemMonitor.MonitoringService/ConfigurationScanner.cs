using Newtonsoft.Json;
using SystemMonitor.Licensing;

namespace SystemMonitor.MonitoringService
{
    public static class ConfigurationScanner
    {
        /// <summary>
        /// Scan a folder for json configuration files
        /// </summary>
        /// <param name="path"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static Configuration ScanFolder(string path, Configuration configuration, NLog.Logger logger)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
                logger.Debug($"{files.Length} monitoring configurations were found in {Path.GetFullPath(path)}.");
                foreach (var file in files)
                {
                    var json = File.ReadAllText(file);
                    try
                    {
                        var hostConfiguration = JsonConvert.DeserializeObject<HostConfiguration>(json);
                        if (hostConfiguration != null)
                            configuration.Services.Add(hostConfiguration);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Skipping configuration for file '{file}'. Check the json for syntax errors.");
                    }
                }
            }
            else
            {
                logger.Warn($"The path '{Path.GetFullPath(path)}' does not exist, no external monitoring configurations were loaded.");
            }

            return configuration;
        }
    }
}
