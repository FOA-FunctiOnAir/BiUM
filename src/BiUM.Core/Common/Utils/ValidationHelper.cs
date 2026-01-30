using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BiUM.Core.Common.Utils;

public static class ValidationHelper
{
    public static bool CheckNull([NotNullWhen(false)] string value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static bool CheckNull([NotNullWhen(false)] Guid? value)
    {
        return value is null || value == Guid.Empty;
    }

    public static bool CheckNull([NotNullWhen(false)] IList<Guid>? array)
    {
        return array is null || array.Count == 0;
    }

    public static bool CheckNull([NotNullWhen(false)] IReadOnlyList<Guid>? array)
    {
        return array is null || array.Count == 0;
    }
}