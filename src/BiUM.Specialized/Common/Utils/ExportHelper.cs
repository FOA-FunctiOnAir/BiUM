using BiUM.Specialized.Common.Models;
using System;
using System.Text.Json;

namespace BiUM.Specialized.Common.Utils;

public static class ExportHelper
{
    public static ExportDto Export<T>(string name, string mimeType, T data, byte[] key)
    {
        var serialized = JsonSerializer.SerializeToUtf8Bytes(data);
        var encrypted = EncryptionHelper.Encrypt(serialized, key);

        return Export(name, mimeType, encrypted);
    }

    public static ExportDto Export(string name, string mimeType, byte[] bytes)
    {
        var val = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);

        return new ExportDto
        {
            Name = name,
            MimeType = mimeType,
            Content = val
        };
    }

    public static T? Import<T>(string content, byte[] key)
    {
        var encrypted = Convert.FromBase64String(content);
        var decrypted = EncryptionHelper.Decrypt(encrypted, key);

        return JsonSerializer.Deserialize<T>(decrypted);
    }
}
