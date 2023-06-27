namespace WallMonitor.Common.IO.Security;

public interface IAesEncryptionService
{
    /// <summary>
    /// Encrypt a string value
    /// </summary>
    /// <param name="plaintextBytes"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<byte[]> EncryptAsync(byte[] plaintextBytes, string key);

    /// <summary>
    /// Decrypt an encrypted value to a string
    /// </summary>
    /// <param name="encryptedBytes"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<byte[]> DecryptAsync(byte[] encryptedBytes, string key);
}