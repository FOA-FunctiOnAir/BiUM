using BiUM.Specialized.Common.Models;
using System.Text;
using System.Text.Json;

namespace BiUM.Specialized.Common.Utils;

public static class ExportHelper
{
    public static ExportDto ExportObject(string name, string mimeType, dynamic data)
    {
        var dataSerialized = JsonSerializer.Serialize(data);
        var dataEncrypted = EncryptionHelper.Encrypt(dataSerialized);
        var dataBytes = Encoding.UTF8.GetBytes(dataEncrypted);
        var dataBase64 = Convert.ToBase64String(dataBytes, Base64FormattingOptions.InsertLineBreaks);

        var response = new ExportDto()
        {
            Name = name,
            MimeType = mimeType,
            Content = dataBase64
        };

        return response;
    }

    public static ExportDto ExportObject(string name, string mimeType, byte[] bytes)
    {
        var val = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);

        return new ExportDto
        {
            Name = name,
            MimeType = mimeType,
            Content = val
        };
    }

    public static TType? ImportObject<TType>(string content)
    {
        var base64EncodedBytes = Convert.FromBase64String(content);
        var dataString = Encoding.UTF8.GetString(base64EncodedBytes);
        var dataDecrypt = EncryptionHelper.Decrypt(dataString);
        var dataOriginal = JsonSerializer.Deserialize<TType>(dataDecrypt);

        return dataOriginal;
    }
}