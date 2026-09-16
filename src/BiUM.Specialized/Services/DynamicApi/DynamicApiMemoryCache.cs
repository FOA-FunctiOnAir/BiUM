using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public sealed class DynamicApiMemoryCache : IDynamicApiMemoryCache
{
    private readonly Guid _dynamicApiId;
    private readonly IMemoryCache _memoryCache;

    public DynamicApiMemoryCache(Guid dynamicApiId, IMemoryCache memoryCache)
    {
        _dynamicApiId = dynamicApiId;
        _memoryCache = memoryCache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _memoryCache.TryGetValue(BuildKey(key), out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _memoryCache.Set(BuildKey(key), value, DynamicApiCacheTtl.Normalize(ttl));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _memoryCache.Remove(BuildKey(key));
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_memoryCache.TryGetValue(BuildKey(key), out _));
    }

    private string BuildKey(string key) => DynamicApiCacheKeyHelper.Build(_dynamicApiId, key);
}