namespace System;

public static partial class Extensions
{
    public static Guid ToGuid(this string source)
    {
        if (Guid.TryParse(source, out var guid))
        {
            return guid;
        }

        return Guid.Empty;
    }

    public static Guid? ToNullableGuid(this string source)
    {
        if (Guid.TryParse(source, out var guid))
        {
            return guid;
        }

        return null;
    }
}