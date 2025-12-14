using BiUM.Core.Models.Caching.Redis;
using StackExchange.Redis;
using System.Text.Json;

internal static class RedisValueExtensions
{
    private static readonly RedisValue _nullValue = "@@NULL";

    public static RedisValue ToRedisValue<T>(this T value)
    {
        var redisValue = _nullValue;
        if (value == null)
            return redisValue;

        var t = typeof(T);
        if (t == typeof(String))
            redisValue = value.ToString();
        else if (t == typeof(Boolean))
            redisValue = Convert.ToBoolean(value);
        else if (t == typeof(Byte))
            redisValue = Convert.ToInt16(value);
        else if (t == typeof(Int16))
            redisValue = Convert.ToInt16(value);
        else if (t == typeof(Int32))
            redisValue = Convert.ToInt32(value);
        else if (t == typeof(Int64))
            redisValue = Convert.ToInt64(value);
        else if (t == typeof(Double))
            redisValue = Convert.ToDouble(value);
        else if (t == typeof(Char))
            redisValue = Convert.ToString(value);
        else if (t == typeof(SByte))
            redisValue = Convert.ToSByte(value);
        else if (t == typeof(UInt16))
            redisValue = Convert.ToUInt32(value);
        else if (t == typeof(UInt32))
            redisValue = Convert.ToUInt32(value);
        else if (t == typeof(UInt64))
            redisValue = Convert.ToUInt64(value);
        else if (t == typeof(Single))
            redisValue = Convert.ToSingle(value);
        else if (t == typeof(Array))
            redisValue = value as byte[];
        else
            redisValue = JsonSerializer.SerializeToUtf8Bytes(value);

        return redisValue;
    }

    public static T ToValueOfType<T>(this RedisValue redisValue)
    {
        T value;
        var type = typeof(T);

        if (type == typeof(Boolean) || type == typeof(string) || type.IsNumericType())
            value = (T)Convert.ChangeType(redisValue, type);
        else if (type == typeof(Boolean?) || type.IsNullableNumericType())
            value = redisValue.IsNull ? default : (T)Convert.ChangeType(redisValue, Nullable.GetUnderlyingType(type));
        else
            return JsonSerializer.Deserialize<T>((byte[])redisValue, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = true });

        return value;
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

    public static CacheItem<T> RedisValueToCacheValue<T>(RedisValue redisValue)
    {
        if (!redisValue.HasValue) return CacheItem<T>.NoValue;
        if (redisValue == _nullValue) return CacheItem<T>.Null;

        try
        {
            var value = redisValue.ToValueOfType<T>();
            return new CacheItem<T>(value, true);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unable to deserialize value {redisValue} to type {typeof(T).FullName} : Error '{e}'");

            return CacheItem<T>.NoValue;
        }
    }
}