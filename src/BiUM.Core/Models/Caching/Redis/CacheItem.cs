using System;

namespace BiUM.Core.Models.Caching.Redis;

public class CacheItem<T>
{
    public CacheItem()
    { }
    public CacheItem(T? value)
    {
        Value = value;
    }
    public CacheItem(T? value, bool hasValue)
    {
        Value = value;
        IsNull = !hasValue;
    }
    public CacheItem(T? value, TimeSpan? expireIn, bool hasValue)
    {
        Value = value;
        ExpireIn = expireIn;
        IsNull = !hasValue;
    }

    public T? Value { get; set; }
    public TimeSpan? ExpireIn { get; set; }

    public static CacheItem<T> Null { get; } = new CacheItem<T>(default, true);
    public static CacheItem<T> NoValue { get; } = new CacheItem<T>(default, false);

    private bool _isNull;
    public bool IsNull
    {
        get { return (Value is null); }
        set { _isNull = (Value is null); }
    }

}