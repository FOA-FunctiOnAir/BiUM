using System.Text;

namespace BiUM.Core.Extensions;

public static class StringExtensions
{
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
}
