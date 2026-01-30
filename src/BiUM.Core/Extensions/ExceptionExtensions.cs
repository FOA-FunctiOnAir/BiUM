namespace System;

public static partial class Extensions
{
    private const string UnknownErrorCode = "unknown_error";

    public static string ToErrorCode(this Exception exception)
    {
        var type = exception.GetType().Name;

        if (type.EndsWith(nameof(Exception), StringComparison.Ordinal))
        {
            type = type.Remove(type.Length - nameof(Exception).Length);
        }

        return string.IsNullOrWhiteSpace(type) ? UnknownErrorCode : type.ToSnakeCase();
    }
}