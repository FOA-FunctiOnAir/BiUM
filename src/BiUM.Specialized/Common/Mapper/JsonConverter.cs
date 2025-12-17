using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BiUM.Specialized.Common.Mapper;

internal static class JsonReadHelpers
{
    public static bool TryReadInt64(ref Utf8JsonReader r, out long value)
    {
        if (r.TokenType == JsonTokenType.Number && r.TryGetInt64(out value)) return true;
        if (r.TokenType == JsonTokenType.String && long.TryParse(r.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return true;

        value = default;

        return false;
    }

    public static bool TryReadDouble(ref Utf8JsonReader r, out double value)
    {
        if (r.TokenType == JsonTokenType.Number && r.TryGetDouble(out value)) return true;
        if (r.TokenType == JsonTokenType.String && double.TryParse(r.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return true;

        value = default;

        return false;
    }

    public static bool TryReadDecimal(ref Utf8JsonReader r, out decimal value)
    {
        if (r.TokenType == JsonTokenType.Number && r.TryGetDecimal(out value)) return true;
        if (r.TokenType == JsonTokenType.String && decimal.TryParse(r.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return true;

        value = default;

        return false;
    }

    public static bool TryReadBooleanLenient(ref Utf8JsonReader r, out bool value)
    {
        switch (r.TokenType)
        {
            case JsonTokenType.True: value = true; return true;
            case JsonTokenType.False: value = false; return true;
            case JsonTokenType.Number:
                if (r.TryGetInt64(out var n)) { value = n != 0; return true; }

                break;
            case JsonTokenType.String:
                var s = r.GetString();
                if (bool.TryParse(s, out var b)) { value = b; return true; }
                if (long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i)) { value = i != 0; return true; }

                break;
        }

        value = default;

        return false;
    }

    public static DateTime FromEpochSmart(long num)
    {
        var isMillis = num > 3_000_000_000L;
        var dto = isMillis
            ? DateTimeOffset.FromUnixTimeMilliseconds(num)
            : DateTimeOffset.FromUnixTimeSeconds(num);

        return dto.UtcDateTime;
    }
}

public sealed class JsonBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return false;
        if (JsonReadHelpers.TryReadBooleanLenient(ref r, out var v)) return v;

        return false;
    }

    public override void Write(Utf8JsonWriter w, bool value, JsonSerializerOptions o)
        => w.WriteBooleanValue(value);
}

public sealed class JsonIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return 0;
        if (JsonReadHelpers.TryReadInt64(ref r, out var v)) return (int)v;

        return 0;
    }

    public override void Write(Utf8JsonWriter w, int v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonLongConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return 0L;
        if (JsonReadHelpers.TryReadInt64(ref r, out var v)) return v;

        return 0L;
    }

    public override void Write(Utf8JsonWriter w, long v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonShortConverter : JsonConverter<short>
{
    public override short Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return (short)0;
        if (JsonReadHelpers.TryReadInt64(ref r, out var v)) return (short)v;

        return (short)0;
    }

    public override void Write(Utf8JsonWriter w, short v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonByteConverter : JsonConverter<byte>
{
    public override byte Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return (byte)0;
        if (JsonReadHelpers.TryReadInt64(ref r, out var v)) return (byte)v;

        return (byte)0;
    }

    public override void Write(Utf8JsonWriter w, byte v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonSByteConverter : JsonConverter<sbyte>
{
    public override sbyte Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return (sbyte)0;
        if (JsonReadHelpers.TryReadInt64(ref r, out var v)) return (sbyte)v;

        return (sbyte)0;
    }

    public override void Write(Utf8JsonWriter w, sbyte v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonUIntConverter : JsonConverter<uint>
{
    public override uint Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return 0U;
        if (JsonReadHelpers.TryReadInt64(ref r, out var v)) return (uint)Math.Max(v, 0);

        return 0U;
    }

    public override void Write(Utf8JsonWriter w, uint v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonULongConverter : JsonConverter<ulong>
{
    public override ulong Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return 0UL;
        if (JsonReadHelpers.TryReadInt64(ref r, out var v)) return v < 0 ? 0UL : (ulong)v;

        return 0UL;
    }

    public override void Write(Utf8JsonWriter w, ulong v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonUShortConverter : JsonConverter<ushort>
{
    public override ushort Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return (ushort)0;
        if (JsonReadHelpers.TryReadInt64(ref r, out var v)) return (ushort)Math.Max(v, 0);

        return (ushort)0;
    }

    public override void Write(Utf8JsonWriter w, ushort v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonFloatConverter : JsonConverter<float>
{
    public override float Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return 0f;
        if (JsonReadHelpers.TryReadDouble(ref r, out var d)) return (float)d;

        return 0f;
    }

    public override void Write(Utf8JsonWriter w, float v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return 0d;
        if (JsonReadHelpers.TryReadDouble(ref r, out var d)) return d;

        return 0d;
    }

    public override void Write(Utf8JsonWriter w, double v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return 0m;
        if (JsonReadHelpers.TryReadDecimal(ref r, out var d)) return d;

        return 0m;
    }

    public override void Write(Utf8JsonWriter w, decimal v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}

public sealed class JsonDateTimeLenientConverter : JsonConverter<DateTime>
{
    private static readonly string[] Formats =
    {
        "O",
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
        "dd.MM.yyyy",
        "dd.MM.yyyy HH:mm",
        "dd.MM.yyyy HH:mm:ss"
    };

    public override DateTime Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return DateTime.MinValue;

        if (r.TokenType == JsonTokenType.Number && r.TryGetInt64(out var num))
            return JsonReadHelpers.FromEpochSmart(num);

        if (r.TokenType == JsonTokenType.String)
        {
            var s = r.GetString();
            if (string.IsNullOrWhiteSpace(s)) return DateTime.MinValue;

            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                return dto.UtcDateTime;

            if (DateTime.TryParseExact(s, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime();

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dtx))
                return DateTime.SpecifyKind(dtx, DateTimeKind.Local).ToUniversalTime();
        }

        return DateTime.MinValue;
    }

    public override void Write(Utf8JsonWriter w, DateTime v, JsonSerializerOptions o)
        => w.WriteStringValue(v.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
}

public sealed class JsonDateTimeOffsetLenientConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return DateTimeOffset.MinValue;

        if (r.TokenType == JsonTokenType.Number && r.TryGetInt64(out var num))
            return JsonReadHelpers.FromEpochSmart(num);

        if (r.TokenType == JsonTokenType.String)
        {
            var s = r.GetString();

            if (string.IsNullOrWhiteSpace(s)) return DateTimeOffset.MinValue;
            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                return dto.ToUniversalTime();

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Local)).ToUniversalTime();
        }

        return DateTimeOffset.MinValue;
    }

    public override void Write(Utf8JsonWriter w, DateTimeOffset v, JsonSerializerOptions o)
        => w.WriteStringValue(v.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
}

#if NET6_0_OR_GREATER

public sealed class JsonDateOnlyLenientConverter : JsonConverter<DateOnly>
{
    private static readonly string[] Formats = { "yyyy-MM-dd", "dd.MM.yyyy", "O" };

    public override DateOnly Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return DateOnly.MinValue;

        if (r.TokenType == JsonTokenType.String)
        {
            var s = r.GetString();

            if (string.IsNullOrWhiteSpace(s)) return DateOnly.MinValue;

            foreach (var f in Formats)
                if (DateOnly.TryParseExact(s, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;

            if (DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2)) return d2;
        }

        return DateOnly.MinValue;
    }

    public override void Write(Utf8JsonWriter w, DateOnly v, JsonSerializerOptions o)
        => w.WriteStringValue(v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}

public sealed class JsonTimeOnlyLenientConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] Formats = { "HH:mm:ss", "HH:mm" };

    public override TimeOnly Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return TimeOnly.MinValue;

        if (r.TokenType == JsonTokenType.String)
        {
            var s = r.GetString();
            if (string.IsNullOrWhiteSpace(s)) return TimeOnly.MinValue;

            foreach (var f in Formats)
                if (TimeOnly.TryParseExact(s, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tOnly)) return tOnly;

            if (TimeOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t2)) return t2;
        }

        return TimeOnly.MinValue;
    }

    public override void Write(Utf8JsonWriter w, TimeOnly v, JsonSerializerOptions o)
        => w.WriteStringValue(v.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
}

#endif

public sealed class JsonGuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return Guid.Empty;

        if (r.TokenType == JsonTokenType.String)
        {
            var s = r.GetString();

            if (Guid.TryParse(s, out var g)) return g;

            return Guid.Empty;
        }

        return Guid.Empty;
    }

    public override void Write(Utf8JsonWriter w, Guid v, JsonSerializerOptions o)
        => w.WriteStringValue(v.ToString());
}

public sealed class JsonEnumNullConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(EnumNullToDefaultConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class EnumNullToDefaultConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
        {
            if (r.TokenType == JsonTokenType.Null) return default;

            if (r.TokenType == JsonTokenType.Number && r.TryGetInt64(out var n))
                return (TEnum)Enum.ToObject(typeof(TEnum), n);

            if (r.TokenType == JsonTokenType.String)
            {
                var s = r.GetString();
                if (string.IsNullOrWhiteSpace(s)) return default;

                if (long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                    return (TEnum)Enum.ToObject(typeof(TEnum), num);

                if (Enum.TryParse<TEnum>(s, true, out var byName))
                    return byName;
            }

            return default;
        }

        public override void Write(Utf8JsonWriter w, TEnum value, JsonSerializerOptions o)
        {
            w.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
    }
}

public sealed class JsonTimeSpanConverter : JsonConverter<TimeSpan>
{
    private static readonly string[] Formats = { "c", "g", "G" };

    public override TimeSpan Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return TimeSpan.Zero;

        if (r.TokenType == JsonTokenType.String)
        {
            var s = r.GetString();
            if (string.IsNullOrWhiteSpace(s)) return TimeSpan.Zero;

            foreach (var f in Formats)
                if (TimeSpan.TryParseExact(s, f, CultureInfo.InvariantCulture, out var ts)) return ts;

            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts2)) return ts2;
        }

        if (r.TokenType == JsonTokenType.Number && r.TryGetInt64(out var num))
        {
            return TimeSpan.FromMilliseconds(num);
        }

        return TimeSpan.Zero;
    }

    public override void Write(Utf8JsonWriter w, TimeSpan v, JsonSerializerOptions o)
        => w.WriteStringValue(v.ToString("c", CultureInfo.InvariantCulture));
}

public sealed class JsonNullToEmptyListConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(List<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var convType = typeof(NullToEmptyListConverter<>).MakeGenericType(elementType);

        return (JsonConverter)Activator.CreateInstance(convType)!;
    }

    private sealed class NullToEmptyListConverter<T> : JsonConverter<List<T>>
    {
        public override List<T> Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
        {
            if (r.TokenType == JsonTokenType.Null) return new();

            var list = JsonSerializer.Deserialize<List<T>>(ref r, o);

            return list ?? new();
        }

        public override void Write(Utf8JsonWriter w, List<T> value, JsonSerializerOptions o)
            => JsonSerializer.Serialize(w, value, o);
    }
}

public sealed class JsonNullableBoolConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null)
            return null;

        if (r.TokenType == JsonTokenType.True)
            return true;

        if (r.TokenType == JsonTokenType.False)
            return false;

        if (r.TokenType == JsonTokenType.String)
        {
            var s = r.GetString()?.Trim();
            if (string.IsNullOrEmpty(s))
                return null;

            if (bool.TryParse(s, out var b))
                return b;

            if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
                return i != 0;

            return null;
        }

        if (r.TokenType == JsonTokenType.Number)
        {
            if (r.TryGetInt64(out var n))
                return n != 0;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter w, bool? value, JsonSerializerOptions o)
    {
        if (value.HasValue)
            w.WriteBooleanValue(value.Value);
        else
            w.WriteNullValue();
    }
}