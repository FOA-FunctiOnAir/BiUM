using BiUM.Core.Common.Utils;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class UserTimeAttribute : JsonConverterAttribute
{
    public override JsonConverter? CreateConverter(Type typeToConvert)
    {
        if (typeToConvert == typeof(DateTime))
        {
            return new UserDateTimeConverter();
        }

        if (typeToConvert == typeof(DateTime?))
        {
            return new UserNullableDateTimeConverter();
        }

        if (typeToConvert == typeof(DateTimeOffset))
        {
            return new UserDateTimeOffsetConverter();
        }

        if (typeToConvert == typeof(DateTimeOffset?))
        {
            return new UserNullableDateTimeOffsetConverter();
        }

        throw new InvalidOperationException($"{nameof(UserTimeAttribute)} supports DateTime and DateTimeOffset properties only, not {typeToConvert}.");
    }
}

internal static class UserTimeJson
{
    private const string WallClockFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF";

    public static void Write(Utf8JsonWriter writer, DateTime utc)
    {
        var zone = UserTimeZone.Current;

        if (zone is null)
        {
            writer.WriteStringValue(UserTimeZone.AsUtc(utc).ToString("O", CultureInfo.InvariantCulture));

            return;
        }

        writer.WriteStringValue(UserTimeZone.ToUserTime(utc, zone).ToString(WallClockFormat, CultureInfo.InvariantCulture));
    }

    public static DateTime ReadUtc(ref Utf8JsonReader reader)
    {
        var text = reader.GetString();

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new JsonException("A date and time value is required.");
        }

        if (HasOffset(text) && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var instant))
        {
            return instant.UtcDateTime;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var wallClock))
        {
            return UserTimeZone.ToUtc(DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified), UserTimeZone.Current);
        }

        throw new JsonException($"'{text}' is not a valid date and time.");
    }

    private static bool HasOffset(string text)
    {
        var timeIndex = text.IndexOf('T');

        if (timeIndex < 0)
        {
            return false;
        }

        var time = text.AsSpan(timeIndex + 1);

        return time.EndsWith("Z", StringComparison.OrdinalIgnoreCase) || time.IndexOfAny('+', '-') >= 0;
    }
}

internal sealed class UserDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => UserTimeJson.ReadUtc(ref reader);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => UserTimeJson.Write(writer, value);
}

internal sealed class UserNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override bool HandleNull => true;

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Null ? null : UserTimeJson.ReadUtc(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();

            return;
        }

        UserTimeJson.Write(writer, value.Value);
    }
}

internal sealed class UserDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(UserTimeJson.ReadUtc(ref reader));

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => UserTimeJson.Write(writer, value.UtcDateTime);
}

internal sealed class UserNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override bool HandleNull => true;

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Null ? null : new DateTimeOffset(UserTimeJson.ReadUtc(ref reader));
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();

            return;
        }

        UserTimeJson.Write(writer, value.Value.UtcDateTime);
    }
}