namespace WallMonitor.Common.Notifications
{
    /// <summary>
    /// Specify the AWS Credentials type
    /// </summary>
    public enum AwsCredentialsType
    {
        /// <summary>
        /// No specific credentials will be used
        /// </summary>
        None,
        /// <summary>
        /// Use an AWS Instance profile
        /// </summary>
        InstanceProfile,
        /// <summary>
        /// Stored profile in C:\Users\<user>\.aws\credentials
        /// </summary>
        StoredProfile,
        /// <summary>
        /// Basic credentials using ApiKey and ApiAccessKey
        /// </summary>
        Basic
    }
}
