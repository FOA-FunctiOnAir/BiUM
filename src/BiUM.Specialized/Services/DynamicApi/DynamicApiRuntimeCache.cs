using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Runtime.Loader;

namespace BiUM.Specialized.Services.DynamicApi;

public sealed class DynamicApiRuntimeCache
{
    private readonly IMemoryCache _memoryCache;

    public DynamicApiRuntimeCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public IDynamicApiHandler GetOrLoad(string cacheKey, byte[] assemblyBytes, string entryPointTypeName)
    {
        return _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromHours(1));
            var loadContext = new DynamicApiAssemblyLoadContext();
            var assembly = loadContext.LoadFromStream(new MemoryStream(assemblyBytes));
            var type = assembly.GetType(entryPointTypeName, throwOnError: true)!;
            return (IDynamicApiHandler)Activator.CreateInstance(type)!;
        })!;
    }

    public void Invalidate(string cacheKey)
    {
        _memoryCache.Remove(cacheKey);
    }

    private sealed class DynamicApiAssemblyLoadContext : AssemblyLoadContext
    {
        public DynamicApiAssemblyLoadContext() : base(isCollectible: true)
        {
        }
    }
}