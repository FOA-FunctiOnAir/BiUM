using BiUM.Core.Common.Utils;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BiUM.Infrastructure.Persistence.ValueConverters;

public sealed class EncryptedDataValueConverter : ValueConverter<string?, string?>
{
    public EncryptedDataValueConverter(string encryptionKey, bool reversible)
        : base(
            v => ToProvider(v, encryptionKey, reversible),
            v => FromProvider(v, encryptionKey, reversible))
    {
    }

    private static string? ToProvider(string? value, string key, bool reversible)
    {
        return value is null ? null : EncryptionHelper.Protect(value, key, reversible);
    }

    private static string? FromProvider(string? value, string key, bool reversible)
    {
        return value is null ? null : EncryptionHelper.Unprotect(value, key, reversible);
    }
}