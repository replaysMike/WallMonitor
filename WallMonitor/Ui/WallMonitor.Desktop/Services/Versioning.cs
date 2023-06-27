using System.Reflection;

namespace WallMonitor.Desktop.Services
{
    public static class Versioning
    {
        /// <summary>
        /// Get the current version of the application
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var versionStr = assembly.GetName()?.Version?.ToString(3) ?? "0.0.0";
            return versionStr;
        }
    }
}
