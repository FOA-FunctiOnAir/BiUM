using System;

namespace BiUM.Infrastructure.Common.Services;

public interface IDateTimeService
{
    DateTime Now { get; }
    DateOnly Today { get; }
    TimeOnly TimeNow { get; }
}