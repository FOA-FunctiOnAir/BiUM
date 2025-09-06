using System.Security.Cryptography;
using System.Text;

namespace BiUM.Specialized.Common.Utils;

public static class EncryptionHelper
{
    public static string Encrypt(string clearText)
    {
        string EncryptionKey = "abc123";
        byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);

        using (var encryptor = Aes.Create())
        {
            var pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);

            using var ms = new MemoryStream();

            using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(clearBytes, 0, clearBytes.Length);
                cs.Close();
            }

            clearText = Convert.ToBase64String(ms.ToArray());
        }

        return clearText;
    }

    public static string Decrypt(string encryptedText)
    {
        string EncryptionKey = "abc123";
        encryptedText = encryptedText.Replace(" ", "+");
        byte[] cipherBytes = Convert.FromBase64String(encryptedText);

        using (var encryptor = Aes.Create())
        {
            var pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);

            using var ms = new MemoryStream();

            using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(cipherBytes, 0, cipherBytes.Length);
                cs.Close();
            }

            encryptedText = Encoding.Unicode.GetString(ms.ToArray());
        }

        return encryptedText;
    }
}