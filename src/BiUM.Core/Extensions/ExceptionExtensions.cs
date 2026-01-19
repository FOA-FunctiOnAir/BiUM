using System.Text;

namespace System;

public static partial class Extensions
{
    public static string GetFullMessage(this Exception exception, int maxLevels = 4)
    {
        var sb = new StringBuilder();

        var level = 0;

        var currentException = exception;

        while (currentException is not null && level < 4)
        {
            sb.AppendLine($"[Level {level}] {currentException.GetType().FullName}");
            sb.AppendLine($"Message: {currentException.Message}");
            sb.AppendLine($"StackTrace: {currentException.StackTrace}");
            sb.AppendLine(new string('-', 3));

            currentException = currentException.InnerException;

            level++;
        }

        return sb.ToString();
    }
}
