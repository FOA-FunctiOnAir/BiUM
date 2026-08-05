using BiUM.Contract.Models.Caching.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Text.Json;

internal static class RedisValueExtensions
{
    private static readonly RedisValue NullValue = "@@NULL";

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    public static RedisValue ToRedisValue<T>(this T value)
    {
        var redisValue = NullValue;

        if (value is null)
        {
            return redisValue;
        }

        var t = typeof(T);

        if (t == typeof(string))
        {
            redisValue = value.ToString();
        }
        else if (t == typeof(bool))
        {
            redisValue = Convert.ToBoolean(value);
        }
        else if (t == typeof(byte))
        {
            redisValue = Convert.ToInt16(value);
        }
        else if (t == typeof(short))
        {
            redisValue = Convert.ToInt16(value);
        }
        else if (t == typeof(int))
        {
            redisValue = Convert.ToInt32(value);
        }
        else if (t == typeof(long))
        {
            redisValue = Convert.ToInt64(value);
        }
        else if (t == typeof(double))
        {
            redisValue = Convert.ToDouble(value);
        }
        else if (t == typeof(char))
        {
            redisValue = Convert.ToString(value);
        }
        else if (t == typeof(sbyte))
        {
            redisValue = Convert.ToSByte(value);
        }
        else if (t == typeof(ushort))
        {
            redisValue = Convert.ToUInt32(value);
        }
        else if (t == typeof(uint))
        {
            redisValue = Convert.ToUInt32(value);
        }
        else if (t == typeof(ulong))
        {
            redisValue = Convert.ToUInt64(value);
        }
        else if (t == typeof(float))
        {
            redisValue = Convert.ToSingle(value);
        }
        else if (t == typeof(Array))
        {
            redisValue = value as byte[];
        }
        else
        {
            redisValue = JsonSerializer.SerializeToUtf8Bytes(value);
        }

        return redisValue;
    }

    public static T ToValueOfType<T>(this RedisValue redisValue)
    {
        var type = typeof(T);

        if (type == typeof(bool) || type == typeof(string) || type.IsNumericType())
        {
            return (T)Convert.ChangeType(redisValue, type);
        }

        if (type == typeof(bool?) || type.IsNullableNumericType())
        {
            return redisValue.IsNull ? default! : (T)Convert.ChangeType(redisValue, Nullable.GetUnderlyingType(type)!);
        }

        return JsonSerializer.Deserialize<T>((byte[])redisValue!, DeserializeOptions)!;
    }

    public static bool IsNumericType(this object o)
    {
        switch (Type.GetTypeCode(o.GetType()))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    public static bool IsNullableNumericType(this object o)
    {
        switch (Type.GetTypeCode(Nullable.GetUnderlyingType(o.GetType())))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    public static CacheItem<T> RedisValueToCacheValue<T>(RedisValue redisValue, ILogger? logger = null, string? key = null)
    {
        if (!redisValue.HasValue)
        {
            return CacheItem<T>.NoValue;
        }

        if (redisValue == NullValue)
        {
            return CacheItem<T>.Null;
        }

        try
        {
            var value = redisValue.ToValueOfType<T>();

            return new CacheItem<T>(value, true);
        }
        catch (Exception e)
        {
            if (logger is not null)
            {
                logger.LogError(e, "Redis value for key '{Key}' could not be deserialized to type {Type}; treating it as unavailable rather than using corrupt/incompatible data.", key ?? "(unknown)", typeof(T).FullName);
            }
            else
            {
                Console.WriteLine($"Unable to deserialize value {redisValue} to type {typeof(T).FullName} : Error '{e}'");
            }

            return CacheItem<T>.NoValue;
        }
    }
}