namespace System;

public static partial class Extensions
{
    public static Guid? ToGuid(this string source)
    {
        if (Guid.TryParse(source, out var guid))
        {
            return guid;
        }

        return null;
    }
}