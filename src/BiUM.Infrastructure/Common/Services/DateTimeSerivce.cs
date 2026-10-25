using BiUM.Core.Authorization;
using System;
using UserTimeZoneHelper = BiUM.Core.Common.Utils.UserTimeZone;

namespace BiUM.Infrastructure.Common.Services;

public class DateTimeService : IDateTimeService
{
    private readonly ICorrelationContextAccessor? _correlationContextAccessor;

    public DateTimeService(ICorrelationContextAccessor? correlationContextAccessor = null)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public DateTime Now => DateTime.UtcNow;
    public DateTimeOffset OffsetNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(Now);
    public DateOnly OffsetToday => DateOnly.FromDateTime(OffsetNow.DateTime);
    public TimeOnly TimeNow => TimeOnly.FromDateTime(Now);
    public TimeOnly OffsetTimeNow => TimeOnly.FromDateTime(OffsetNow.DateTime);

    public TimeZoneInfo UserTimeZone => CurrentZone ?? TimeZoneInfo.Utc;
    public DateTime UserNow => UserTimeZoneHelper.ToUserTime(DateTime.UtcNow, CurrentZone);
    public DateOnly UserToday => DateOnly.FromDateTime(UserNow);

    public DateTime ToUserTime(DateTime utc) => UserTimeZoneHelper.ToUserTime(utc, CurrentZone);
    public DateTime ToUserTime(DateTimeOffset value) => UserTimeZoneHelper.ToUserTime(value, CurrentZone);
    public DateTime ToUtc(DateTime userTime) => UserTimeZoneHelper.ToUtc(userTime, CurrentZone);

    private TimeZoneInfo? CurrentZone => UserTimeZoneHelper.Resolve(_correlationContextAccessor?.CorrelationContext?.TimeZoneId);
}