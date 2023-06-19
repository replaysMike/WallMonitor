namespace SystemMonitor.Common.Notifications
{
    /// <summary>
    /// Aws Api/Sdk Credentials
    /// </summary>
    public class AwsCredentials
    {
        /// <summary>
        /// Specify the profile name for AwsCredentialsType = StoredProfile
        /// </summary>
        public string ProfileName { get; set; } = "default";

        /// <summary>
        /// Specify the profile name for AwsCredentialsType = Basic
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Specify the profile name for AwsCredentialsType = Basic
        /// </summary>
        public string SecretKey { get; set; }
    }
}
