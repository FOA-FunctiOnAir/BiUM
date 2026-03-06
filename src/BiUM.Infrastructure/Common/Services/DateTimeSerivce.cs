using System;

namespace BiUM.Infrastructure.Common.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime Now => DateTime.UtcNow;
    public DateTimeOffset OffsetNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(Now);
    public DateOnly OffsetToday => DateOnly.FromDateTime(OffsetNow.DateTime);
    public TimeOnly TimeNow => TimeOnly.FromDateTime(Now);
    public TimeOnly OffsetTimeNow => TimeOnly.FromDateTime(OffsetNow.DateTime);
}