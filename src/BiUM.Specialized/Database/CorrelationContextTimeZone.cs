using BiUM.Core.Authorization;
using BiUM.Infrastructure.Common.Models;
using System;
using System.Collections.Generic;

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

    // ProjectTo-based queries (Extensions.First/List/PaginatedList/Where.cs) translate straight to
    // SQL, so an AutoMapper AfterMap hook never runs for them -- Created/CreatedTime/Updated/UpdatedTime
    // still arrive via ordinary convention mapping, this just derives CreatedAt/UpdatedAt from those
    // already-materialized values once the query result is back in memory.
    public static T? PopulateAuditFields<T>(T? destination)
    {
        if (destination is BaseDto dto)
        {
            dto.CreatedAt = ConvertToLocal(dto.Created, dto.CreatedTime);
            dto.UpdatedAt = ConvertToLocalOrNull(dto.Updated, dto.UpdatedTime);
        }

        return destination;
    }

    public static List<T> PopulateAuditFields<T>(List<T> destinations)
    {
        foreach (var destination in destinations)
        {
            PopulateAuditFields(destination);
        }

        return destinations;
    }
}