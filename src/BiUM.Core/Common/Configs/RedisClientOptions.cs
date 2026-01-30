using System;

namespace BiUM.Core.Common.Configs;

public class RedisClientOptions
{
    public const string Name = "RedisClientOptions";

    public bool Enable { get; set; }
    public TimeSpan? DefaultCacheTimeout { get; set; }
    public string? ConnectionString { get; set; }
}