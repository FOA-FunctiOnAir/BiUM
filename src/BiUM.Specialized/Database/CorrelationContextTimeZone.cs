using BiUM.Core.Authorization;
using System;

namespace BiUM.Specialized.Database;

internal static class CorrelationContextTimeZone
{
    private static ICorrelationContextAccessor? _accessor;

    public static void Configure(ICorrelationContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public static DateTimeOffset ConvertToLocal(DateOnly date, TimeOnly time)
    {
        var utcDt = new DateTime(date, time, DateTimeKind.Utc);
        var tzId = _accessor?.CorrelationContext?.TimeZoneId;

        if (string.IsNullOrEmpty(tzId))
        {
            return new DateTimeOffset(utcDt);
        }

        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        return TimeZoneInfo.ConvertTimeFromUtc(utcDt, tz);
    }

    public static DateTimeOffset? ConvertToLocalOrNull(DateOnly? date, TimeOnly? time)
    {
        if (date is null || time is null)
        {
            return null;
        }

        return ConvertToLocal(date.Value, time.Value);
    }
}