using BiUM.Core.Authorization;
using System;
using System.Collections.Concurrent;

namespace BiUM.Core.Common.Utils;

public static class UserTimeZone
{
    private static readonly ConcurrentDictionary<string, TimeZoneInfo?> Cache = new(StringComparer.OrdinalIgnoreCase);

    private static ICorrelationContextAccessor? _accessor;

    public static void Configure(ICorrelationContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public static TimeZoneInfo? Current => Resolve(_accessor?.CorrelationContext?.TimeZoneId);

    public static TimeZoneInfo? Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        return Cache.GetOrAdd(timeZoneId.Trim(), static id => TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) ? zone : null);
    }

    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime ToUserTime(DateTime utc, TimeZoneInfo? zone)
    {
        var value = AsUtc(utc);

        return zone is null ? value : DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(value, zone), DateTimeKind.Unspecified);
    }

    public static DateTime ToUserTime(DateTimeOffset value, TimeZoneInfo? zone) => ToUserTime(value.UtcDateTime, zone);

    public static DateTime ToUtc(DateTime userTime, TimeZoneInfo? zone)
    {
        if (userTime.Kind == DateTimeKind.Utc)
        {
            return userTime;
        }

        if (userTime.Kind == DateTimeKind.Local)
        {
            return userTime.ToUniversalTime();
        }

        if (zone is null)
        {
            return DateTime.SpecifyKind(userTime, DateTimeKind.Utc);
        }

        var local = DateTime.SpecifyKind(userTime, DateTimeKind.Unspecified);

        while (zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(30);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }

    public static DateTimeOffset ToUserOffset(DateTime utc, TimeZoneInfo? zone)
    {
        var value = AsUtc(utc);

        return zone is null ? new DateTimeOffset(value) : TimeZoneInfo.ConvertTime(new DateTimeOffset(value), zone);
    }
}