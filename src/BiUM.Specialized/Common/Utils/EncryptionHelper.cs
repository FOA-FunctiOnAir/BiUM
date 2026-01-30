using System;
using System.IO;
using System.Security.Cryptography;

namespace BiUM.Specialized.Common.Utils;

public static class EncryptionHelper
{
    private const int Iterations = 256_789;

    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32;  // 256 bits (AES-256)
    private const int IvSize = 16;   // 128 bits (Default AES block size, don't change)

    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public static byte[] Encrypt(byte[] value, byte[] password)
    {
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
}