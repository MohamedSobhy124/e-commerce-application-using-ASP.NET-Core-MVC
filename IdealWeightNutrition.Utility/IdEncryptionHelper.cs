using System;
using System.Security.Cryptography;
using System.Text;

namespace IdealWeightNutrition.Utility
{
    /// <summary>
    /// Helper class for encrypting and decrypting IDs in URLs for security
    /// </summary>
    public static class IdEncryptionHelper
    {
        // Encryption key - should be stored in configuration in production
        // For production, use a strong key from appsettings.json
        private static readonly string EncryptionKey = "IdealWeightNutrition2025SecureKey!@#$%^&*()1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// Encrypts an integer ID to a URL-safe string
        /// </summary>
        public static string EncryptId(int id)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    // Derive key and IV from the encryption key
                    var key = DeriveKey(EncryptionKey, 32);
                    var iv = DeriveKey(EncryptionKey + "IV", 16);
                    
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Convert ID to bytes
                    byte[] idBytes = Encoding.UTF8.GetBytes(id.ToString());
                    
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(idBytes, 0, idBytes.Length);
                        
                        // Convert to base64 and make URL-safe
                        string encrypted = Convert.ToBase64String(encryptedBytes);
                        return encrypted.Replace('+', '-').Replace('/', '_').Replace("=", "");
                    }
                }
            }
            catch
            {
                // If encryption fails, return a hashed version as fallback
                return HashId(id);
            }
        }

        /// <summary>
        /// Decrypts an encrypted ID string back to an integer
        /// </summary>
        public static int? DecryptId(string encryptedId)
        {
            if (string.IsNullOrWhiteSpace(encryptedId))
                return null;

            try
            {
                // Restore base64 padding and URL-safe characters
                string base64 = encryptedId.Replace('-', '+').Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                byte[] encryptedBytes = Convert.FromBase64String(base64);

                using (Aes aes = Aes.Create())
                {
                    var key = DeriveKey(EncryptionKey, 32);
                    var iv = DeriveKey(EncryptionKey + "IV", 16);
                    
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        string decrypted = Encoding.UTF8.GetString(decryptedBytes);
                        
                        if (int.TryParse(decrypted, out int id))
                            return id;
                    }
                }
            }
            catch
            {
                // Try to decode from hash if encryption fails
                return DecryptFromHash(encryptedId);
            }

            return null;
        }

        /// <summary>
        /// Derives a key of specified length from a password
        /// </summary>
        private static byte[] DeriveKey(string password, int keyLength)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                byte[] key = new byte[keyLength];
                Array.Copy(hash, 0, key, 0, Math.Min(hash.Length, keyLength));
                
                // If we need more bytes, hash again with salt
                if (keyLength > hash.Length)
                {
                    for (int i = hash.Length; i < keyLength; i++)
                    {
                        byte[] extendedHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + i));
                        key[i] = extendedHash[0];
                    }
                }
                
                return key;
            }
        }

        /// <summary>
        /// Fallback: Hash ID for one-way encoding (less secure but always works)
        /// </summary>
        private static string HashId(int id)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(id.ToString() + EncryptionKey));
                string base64 = Convert.ToBase64String(hash);
                return base64.Substring(0, 16).Replace('+', '-').Replace('/', '_');
            }
        }

        /// <summary>
        /// Fallback: Try to decode from hash (not perfect but better than nothing)
        /// </summary>
        private static int? DecryptFromHash(string hashedId)
        {
            // This is a fallback - we can't truly decrypt a hash
            // In production, you might want to maintain a lookup table
            // For now, return null to indicate failure
            return null;
        }

        /// <summary>
        /// Validates if an encrypted ID string is valid format
        /// </summary>
        public static bool IsValidEncryptedId(string encryptedId)
        {
            if (string.IsNullOrWhiteSpace(encryptedId))
                return false;

            // Check if it looks like a base64 string (URL-safe)
            return encryptedId.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }
    }
}

