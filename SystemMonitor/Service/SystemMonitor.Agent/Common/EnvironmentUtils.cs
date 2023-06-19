namespace SystemMonitor.Agent.Common
{
    public static class EnvironmentUtils
    {
        /// <summary>
        /// Returns true if running in a docker container
        /// </summary>
        public static bool InDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }
}
