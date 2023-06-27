namespace WallMonitor.Common.Notifications
{
    /// <summary>
    /// AWS SES email configuration
    /// </summary>
    public class AwsSesEmailConfiguration
    {
        /// <summary>
        /// Aws Region
        /// </summary>
        public string AwsRegion { get; set; } = "us-east-1";

        /// <summary>
        /// Specify the type of Aws credentials to use
        /// </summary>
        public AwsCredentialsType CredentialsType { get; set; } = AwsCredentialsType.Basic;

        /// <summary>
        /// Specify the Aws credentials to use
        /// </summary>
        public AwsCredentials Credentials { get; set; } = new AwsCredentials();

        /// <summary>
        /// Paths for specified features
        /// </summary>
        public Dictionary<string, string> FeaturePaths { get; set; } = new Dictionary<string, string>();
    }
}
