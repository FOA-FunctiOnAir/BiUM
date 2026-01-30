using System.Text;

namespace System;

public static partial class Extensions
{
    public static Guid ToGuid(this string source)
    {
        return Guid.TryParse(source, out var guid) ? guid : Guid.Empty;
    }

    public static Guid? ToNullableGuid(this string source)
    {
        return Guid.TryParse(source, out var guid) ? guid : null;
    }

    public static string ToSnakeCase(this string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
            {
                if (i > 0)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(text[i]));
            }
            else
            {
                sb.Append(text[i]);
            }
        }

        return sb.ToString();
    }

    public static string ToDotNotation(this string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
            {
                if (i > 0)
                {
                    sb.Append('.');
                }

                sb.Append(char.ToLowerInvariant(text[i]));
            }
            else
            {
                sb.Append(text[i]);
            }
        }

        return sb.ToString();
    }
}