using BiUM.Core.Caching.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public sealed class DynamicApiCache : IDynamicApiCache
{
    private readonly Guid _dynamicApiId;
    private readonly IRedisClient? _redisClient;

    public DynamicApiCache(Guid dynamicApiId, IRedisClient? redisClient)
    {
        _dynamicApiId = dynamicApiId;
        _redisClient = redisClient;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var redisClient = RequireRedisClient();
        var item = await redisClient.GetAsync<T>(BuildKey(key));

        if (ReferenceEquals(item, BiUM.Contract.Models.Caching.Redis.CacheItem<T>.NoValue))
        {
            return default;
        }

        return item.Value;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var redisClient = RequireRedisClient();
        return redisClient.AddAsync(BuildKey(key), value, DynamicApiCacheTtl.Normalize(ttl));
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var redisClient = RequireRedisClient();
        return redisClient.RemoveAsync(BuildKey(key));
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var redisClient = RequireRedisClient();
        return redisClient.ExistsAsync(BuildKey(key));
    }

    private string BuildKey(string key) => DynamicApiCacheKeyHelper.Build(_dynamicApiId, key);

    private IRedisClient RequireRedisClient()
    {
        if (_redisClient is null)
        {
            throw new InvalidOperationException("dynamic_api_cache_redis_unavailable");
        }

        return _redisClient;
    }
}