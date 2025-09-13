namespace BiUM.Specialized.Common.Utils;

public static class DateTimeHelper
{
    public static DateOnly GetDateNow()
    {
        var today = DateTime.UtcNow;

        return DateOnly.FromDateTime(today);
    }

    public static TimeOnly GetTimeNow()
    {
        var today = DateTime.UtcNow;

        return TimeOnly.FromDateTime(today);
    }
}