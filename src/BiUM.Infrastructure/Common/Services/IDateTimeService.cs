using System;

namespace BiUM.Infrastructure.Common.Services;

public interface IDateTimeService
{
    public DateTime Now { get; }
    public DateTimeOffset OffsetNow { get; }
    public DateOnly Today { get; }
    public DateOnly OffsetToday { get; }
    public TimeOnly TimeNow { get; }
    public TimeOnly OffsetTimeNow { get; }

    public TimeZoneInfo UserTimeZone { get; }
    public DateTime UserNow { get; }
    public DateOnly UserToday { get; }

    public DateTime ToUserTime(DateTime utc);
    public DateTime ToUserTime(DateTimeOffset value);
    public DateTime ToUtc(DateTime userTime);
}