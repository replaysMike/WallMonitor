using System.Security.Cryptography;
using System.Text;

namespace SystemMonitor.Common.IO.Security
{
    public class AesEncryptionService : IAesEncryptionService
    {
        private const int Bits = 256;

        /// <summary>
        /// Encrypt a string value
        /// </summary>
        /// <param name="plaintextBytes"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<byte[]> EncryptAsync(byte[] plaintextBytes, string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            // Use the password to encrypt the plaintext
            using var encryptor = Aes.Create();
            encryptor.Padding = PaddingMode.PKCS7;
            encryptor.Mode = CipherMode.CBC;
            encryptor.KeySize = Bits;
            encryptor.Key = DeriveKeyFromPassword(key, Bits / 8);
            var iv = DeriveKeyFromPassword(key.Reverse().ToString(), encryptor.BlockSize / 8);
            encryptor.IV = iv;
            using var ms = new MemoryStream();
            await using var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(plaintextBytes, 0, plaintextBytes.Length);
            await cs.FlushFinalBlockAsync();
            return ms.ToArray();
        }

        /// <summary>
        /// Decrypt an encrypted value to a string
        /// </summary>
        /// <param name="encryptedBytes"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<byte[]> DecryptAsync(byte[] encryptedBytes, string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            // Use the password to decrypt the encrypted string
            var encryptor = Aes.Create();
            encryptor.Padding = PaddingMode.PKCS7;
            encryptor.Mode = CipherMode.CBC;
            encryptor.KeySize = Bits;
            encryptor.Key = DeriveKeyFromPassword(key, Bits / 8);
            var iv = DeriveKeyFromPassword(key.Reverse().ToString(), encryptor.BlockSize / 8);
            encryptor.IV = iv;
            using var ms = new MemoryStream();
            await using var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(encryptedBytes, 0, encryptedBytes.Length);
            await cs.FlushFinalBlockAsync();
            return ms.ToArray();
        }

        private static byte[] DeriveKeyFromPassword(string key, int keyLength)
        {
            var salt = new byte [] { 0x01, 0x06, 0xF3, 0xE5, 0x2A, 0xB7 };
            const int iterations = 1000;
            var hashMethod = HashAlgorithmName.SHA384;
            return Rfc2898DeriveBytes.Pbkdf2(Encoding.Unicode.GetBytes(key), salt, iterations, hashMethod, keyLength);
        }
    }
}
