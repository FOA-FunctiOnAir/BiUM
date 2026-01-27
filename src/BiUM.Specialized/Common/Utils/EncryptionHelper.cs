using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BiUM.Specialized.Common.Utils;

public static class EncryptionHelper
{
    private const int Iterations = 1000;
    private const string Key = "abc123";

    private static readonly byte[] Salt = "Ivan Medvedev"u8.ToArray();
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA1;

    private static readonly byte[] EncryptionKey = Rfc2898DeriveBytes.Pbkdf2(Key, Salt, Iterations, HashAlgorithm, 32);
    private static readonly byte[] EncryptionIV = Rfc2898DeriveBytes.Pbkdf2(EncryptionKey, Salt, Iterations, HashAlgorithm, 16);

    public static string Encrypt(string clearText)
    {
        var clearBytes = Encoding.Unicode.GetBytes(clearText);

        using var encryptor = Aes.Create();

        encryptor.Key = EncryptionKey;
        encryptor.IV = EncryptionIV;

        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write);

        cs.Write(clearBytes, 0, clearBytes.Length);
        cs.Close();

        clearText = Convert.ToBase64String(ms.ToArray());

        return clearText;
    }

    public static string Decrypt(string encryptedText)
    {
        encryptedText = encryptedText.Replace(" ", "+");

        var cipherBytes = Convert.FromBase64String(encryptedText);

        using var encryptor = Aes.Create();

        encryptor.Key = EncryptionKey;
        encryptor.IV = EncryptionIV;

        using var ms = new MemoryStream();

        using var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write);

        cs.Write(cipherBytes, 0, cipherBytes.Length);
        cs.Close();

        encryptedText = Encoding.Unicode.GetString(ms.ToArray());

        return encryptedText;
    }
}
