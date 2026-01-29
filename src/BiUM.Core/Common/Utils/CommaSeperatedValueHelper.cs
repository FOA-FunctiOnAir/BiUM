using System;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Core.Common.Utils;

public static class CommaSeperatedValueHelper
{
    public static IList<byte> ParseAsBytes(string? text)
        => ParseAs(text, Is<byte>, Parse<byte>);

    public static IList<short> ParseAsShorts(string? text)
        => ParseAs(text, Is<short>, Parse<short>);

    public static IList<int> ParseAsIntegers(string? text)
        => ParseAs(text, Is<int>, Parse<int>);

    public static IList<long> ParseAsLongs(string? text)
        => ParseAs(text, Is<long>, Parse<long>);

    public static IList<float> ParseAsFloats(string? text)
        => ParseAs(text, Is<float>, Parse<float>);

    public static IList<double> ParseAsDoubles(string? text)
        => ParseAs(text, Is<double>, Parse<double>);

    public static IList<decimal> ParseAsDecimals(string? text)
        => ParseAs(text, Is<decimal>, Parse<decimal>);

    public static IList<Guid> ParseAsGuids(string? text)
        => ParseAs(text, Is<Guid>, Parse<Guid>);

    public static IList<DateTimeOffset> ParseAsDateTimeOffsets(string? text)
        => ParseAs(text, Is<DateTimeOffset>, Parse<DateTimeOffset>);

    public static IList<DateTime> ParseAsDateTimes(string? text)
        => ParseAs(text, Is<DateTime>, Parse<DateTime>);

    public static IList<DateOnly> ParseAsDateOnlys(string? text)
        => ParseAs(text, Is<DateOnly>, Parse<DateOnly>);

    public static IList<TimeOnly> ParseAsTimeOnlys(string? text)
        => ParseAs(text, Is<TimeOnly>, Parse<TimeOnly>);

    public static IList<TimeSpan> ParseAsTimeSpans(string? text)
        => ParseAs(text, Is<TimeSpan>, Parse<TimeSpan>);

    public static IList<string> ParseAsStrings(string? text)
        => Split(text);

    public static IList<T> ParseAsEnums<T>(string? text, bool ignoreCase = true)
        where T : struct
    {
        try
        {
            var type = typeof(T);

            if (!type.IsEnum)
                return [];

            bool IsTEnum(string value) => Enum.TryParse<T>(value, ignoreCase, out _);

            T ParseTEnum(string value) => Enum.Parse<T>(value, ignoreCase);

            return ParseAs(text, IsTEnum, ParseTEnum);
        }
        catch
        {
            return [];
        }
    }

    private static IList<T> ParseAs<T>(string? text, Func<string, bool> isParsable, Func<string, T> parse)
    {
        try
        {
            var parts = Split(text);

            if (parts.Count == 0)
                return Array.Empty<T>();

            if (!parts.All(isParsable))
                return [];

            return (IList<T>)parts.Select(parse).ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static IList<string> Split(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : (IList<string>)text.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool Is<TNumber>(string value)
        where TNumber : IParsable<TNumber>
        => TNumber.TryParse(value, null, out _);

    private static TNumber Parse<TNumber>(string value)
        where TNumber : IParsable<TNumber>
        => TNumber.Parse(value, null);
}
