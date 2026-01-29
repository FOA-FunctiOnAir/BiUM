using System;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Core.Common.Utils;

public static class CommaSeparatedValueHelper
{
    public static IReadOnlyList<byte> ParseAsBytes(string? text)
        => ParseAs(text, Is<byte>, Parse<byte>);

    public static IReadOnlyList<short> ParseAsShorts(string? text)
        => ParseAs(text, Is<short>, Parse<short>);

    public static IReadOnlyList<int> ParseAsIntegers(string? text)
        => ParseAs(text, Is<int>, Parse<int>);

    public static IReadOnlyList<long> ParseAsLongs(string? text)
        => ParseAs(text, Is<long>, Parse<long>);

    public static IReadOnlyList<float> ParseAsFloats(string? text)
        => ParseAs(text, Is<float>, Parse<float>);

    public static IReadOnlyList<double> ParseAsDoubles(string? text)
        => ParseAs(text, Is<double>, Parse<double>);

    public static IReadOnlyList<decimal> ParseAsDecimals(string? text)
        => ParseAs(text, Is<decimal>, Parse<decimal>);

    public static IReadOnlyList<Guid> ParseAsGuids(string? text)
        => ParseAs(text, Is<Guid>, Parse<Guid>);

    public static IReadOnlyList<DateTimeOffset> ParseAsDateTimeOffsets(string? text)
        => ParseAs(text, Is<DateTimeOffset>, Parse<DateTimeOffset>);

    public static IReadOnlyList<DateTime> ParseAsDateTimes(string? text)
        => ParseAs(text, Is<DateTime>, Parse<DateTime>);

    public static IReadOnlyList<DateOnly> ParseAsDateOnlys(string? text)
        => ParseAs(text, Is<DateOnly>, Parse<DateOnly>);

    public static IReadOnlyList<TimeOnly> ParseAsTimeOnlys(string? text)
        => ParseAs(text, Is<TimeOnly>, Parse<TimeOnly>);

    public static IReadOnlyList<TimeSpan> ParseAsTimeSpans(string? text)
        => ParseAs(text, Is<TimeSpan>, Parse<TimeSpan>);

    public static IReadOnlyList<string> ParseAsStrings(string? text)
        => Split(text);

    public static IReadOnlyList<T> ParseAsEnums<T>(string? text, bool ignoreCase = true)
        where T : struct, Enum
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var type = typeof(T);

        return type.IsEnum ? ParseAs(text, IsTEnum, ParseTEnum) : [];

        bool IsTEnum(string value) => Enum.TryParse<T>(value, ignoreCase, out _);

        T ParseTEnum(string value) => Enum.Parse<T>(value, ignoreCase);
    }

    private static IReadOnlyList<T> ParseAs<T>(string? text, Func<string, bool> isParsable, Func<string, T> parse)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var parts = Split(text);

        return parts.Count > 0
            ? parts.All(isParsable)
                ? parts.Select(parse).ToArray()
                : []
            : [];
    }

    private static IReadOnlyList<string> Split(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? []
            : (IReadOnlyList<string>)text.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool Is<TNumber>(string? value)
        where TNumber : IParsable<TNumber>
        => TNumber.TryParse(value, null, out _);

    private static TNumber Parse<TNumber>(string value)
        where TNumber : IParsable<TNumber>
        => TNumber.Parse(value, null);
}
