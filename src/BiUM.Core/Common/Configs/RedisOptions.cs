using System;

namespace BiUM.Core.Common.Configs;

public class RedisOptions
{
    public const string Name = "RedisOptions";

    public const string DefaultClientKey = "Default";

    public bool Enable { get; set; }
    public TimeSpan? DefaultCacheTimeout { get; set; }
    public string? ConnectionString { get; set; }
}