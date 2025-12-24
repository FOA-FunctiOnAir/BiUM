using System.Text;

namespace System;

public static partial class Extensions
{
    public static string GetFullMessage(this Exception ex)
    {
        if (ex is null)
        {
            return string.Empty;
        }

        var exception = ex;

        var sb = new StringBuilder();
        var level = 0;

        while (exception is not null && level < 4)
        {
            sb.AppendLine($"[Level {level}] {exception.GetType().FullName}");
            sb.AppendLine($"Message: {exception.Message}");
            sb.AppendLine($"StackTrace: {exception.StackTrace}");
            sb.AppendLine(new string('-', 80));

            exception = exception.InnerException;
            level++;
        }

        return sb.ToString();
    }
}