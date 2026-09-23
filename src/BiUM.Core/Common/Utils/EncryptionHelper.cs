using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BiUM.Core.Common.Utils;

public static class EncryptionHelper
{
    public const string ActivitySourceName = "BiUM.EncryptionHelper";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private const int Iterations = 256_789;

    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32;  // 256 bits (AES-256)
    private const int IvSize = 16;   // 128 bits (Default AES block size, don't change)
    private const int HashSize = 32;  // 256 bits

    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public static byte[] Encrypt(byte[] value, byte[] password)
    {
        using var activity = StartCryptoActivity("Encrypt");

        // 1. Generate a random salt
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // 2. Derive the key from the password and salt
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV(); // Generate a random IV

        var iv = aes.IV;

        using var ms = new MemoryStream();

        // 3. Prepend Salt and IV to the output stream
        ms.Write(salt, 0, salt.Length);
        ms.Write(iv, 0, iv.Length);

        // 4. Encrypt the data
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(value, 0, value.Length);
        }

        return ms.ToArray();
    }

    public static byte[] Decrypt(byte[] value, byte[] password)
    {
        using var activity = StartCryptoActivity("Decrypt");

        using var ms = new MemoryStream(value);

        // 1. Read the Salt
        var salt = new byte[SaltSize];

        if (ms.Read(salt, 0, salt.Length) != salt.Length)
        {
            throw new ArgumentException("Invalid encrypted data: missing salt");
        }

        // 2. Read the IV
        var iv = new byte[IvSize];

        if (ms.Read(iv, 0, iv.Length) != iv.Length)
        {
            throw new ArgumentException("Invalid encrypted data: missing IV");
        }

        // 3. Derive the key using the extracted salt
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        // 4. Decrypt the rest of the stream
        using var outputMs = new MemoryStream();

        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
        {
            cs.CopyTo(outputMs);
        }

        return outputMs.ToArray();
    }

    public static string Encrypt(string plainValue, string key)
    {
        if (string.IsNullOrEmpty(plainValue))
        {
            return plainValue;
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Encryption key cannot be empty.", nameof(key));
        }

        var plainBytes = Encoding.UTF8.GetBytes(plainValue);
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var encrypted = Encrypt(plainBytes, keyBytes);

        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string cipherBase64, string key)
    {
        if (string.IsNullOrEmpty(cipherBase64))
        {
            return cipherBase64;
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Encryption key cannot be empty.", nameof(key));
        }

        var cipherBytes = Convert.FromBase64String(cipherBase64);
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var decrypted = Decrypt(cipherBytes, keyBytes);

        return Encoding.UTF8.GetString(decrypted);
    }

    public static string Hash(string plainValue)
    {
        if (string.IsNullOrEmpty(plainValue))
        {
            return plainValue;
        }

        using var activity = StartCryptoActivity("Hash");

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainValue);
        var hash = Rfc2898DeriveBytes.Pbkdf2(plainBytes, salt, Iterations, HashAlgorithm, HashSize);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string plainValue, string storedValue)
    {
        if (string.IsNullOrEmpty(plainValue) || string.IsNullOrEmpty(storedValue))
        {
            return false;
        }

        var parts = storedValue.Split(':', 2);

        if (parts.Length != 2)
        {
            return false;
        }

        using var activity = StartCryptoActivity("Verify");

        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);
        var plainBytes = Encoding.UTF8.GetBytes(plainValue);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(plainBytes, salt, Iterations, HashAlgorithm, HashSize);
        var verified = CryptographicOperations.FixedTimeEquals(storedHash, computedHash);

        activity?.SetTag("encryption.verify.result", verified);

        return verified;
    }

    private static Activity? StartCryptoActivity(string operation)
    {
        var activity = ActivitySource.StartActivity($"EncryptionHelper.{operation}", ActivityKind.Internal);

        activity?.SetTag("encryption.pbkdf2_iterations", Iterations);

        return activity;
    }

    public static string Protect(string plainValue, string key, bool reversible)
    {
        if (string.IsNullOrEmpty(plainValue))
        {
            return plainValue;
        }

        if (reversible)
        {
            return Encrypt(plainValue, key);
        }

        return Hash(plainValue);
    }

    public static string Unprotect(string storedValue, string key, bool reversible)
    {
        if (string.IsNullOrEmpty(storedValue))
        {
            return storedValue;
        }

        if (reversible)
        {
            if (storedValue.Contains(':'))
            {
                return storedValue;
            }

            return Decrypt(storedValue, key);
        }

        return storedValue;
    }
}