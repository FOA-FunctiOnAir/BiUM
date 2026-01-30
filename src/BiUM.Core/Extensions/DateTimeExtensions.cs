using System.Globalization;

namespace System;

public static partial class Extensions
{
    private const string dateFormat = "yyyy-MM-dd";
    private const string timeFormat = "HH:mm:ss.fffffff";
    private static readonly string[] timeFormats = ["HH:mm", "HH:mm:ss", "hh:mm tt", "hh:mm:ss tt", "HH:mm:ss.fffffff"];

    public static DateOnly ToDateOnly(this string source)
    {
        DateOnly.TryParseExact(source, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);

        return date;
    }

    public static DateOnly? ToNullableDateOnly(this string source)
    {
        var parsed = DateOnly.TryParseExact(source, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);

        return parsed ? date : null;
    }

    public static string ToDateString(this DateOnly source)
    {
        var value = source.ToString(dateFormat);

        return value;
    }

    public static string ToDateString(this DateOnly? source)
    {
        if (source is null)
        {
            return string.Empty;
        }

        var value = source.Value.ToDateString();

        return value;
    }

    public static TimeOnly ToTimeOnly(this string source)
    {
        TimeOnly.TryParseExact(source, timeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var time);

        return time;
    }

    public static TimeOnly? ToNullableTimeOnly(this string source)
    {
        var parsed = TimeOnly.TryParseExact(source, timeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var time);

        return parsed ? time : null;
    }

    public static string ToTimeString(this TimeOnly source)
    {
        var value = source.ToString(timeFormat);

        return value;
    }

    public static string ToTimeString(this TimeOnly? source)
    {
        if (source is null)
        {
            return string.Empty;
        }

        var value = source.Value.ToTimeString();

        return value;
    }
}