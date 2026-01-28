using System;

namespace BiUM.Contract.Models.Caching.Redis;

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


#pragma warning disable CA1000
    public static CacheItem<T> Null { get; } = new(default, true);
    public static CacheItem<T> NoValue { get; } = new(default, false);
#pragma warning restore CA1000

    private bool _isNull;
    public bool IsNull
    {
        get { return Value is null; }
        set { _isNull = Value is null; }
    }
}
