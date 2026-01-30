using System;

namespace BiUM.Infrastructure.Common.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime Now => DateTime.Now;
    public DateOnly Today => DateOnly.FromDateTime(Now);
    public TimeOnly TimeNow => TimeOnly.FromDateTime(Now);
}