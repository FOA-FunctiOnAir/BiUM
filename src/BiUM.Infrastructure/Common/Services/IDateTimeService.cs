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
}