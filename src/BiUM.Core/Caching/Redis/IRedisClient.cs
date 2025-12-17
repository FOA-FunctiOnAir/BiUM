using BiUM.Core.Models.Caching.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiUM.Core.Caching.Redis;

public interface IRedisClient : IDisposable
{
    Task<CacheItem<T>> GetAsync<T>(string key);
    Task<IDictionary<string, CacheItem<T>>> GetAllAsync<T>(IEnumerable<string> keys);
    Task<bool> AddAsync<T>(string key, T value, TimeSpan? expiresIn = null);
    Task<bool> RemoveAsync(string key);
    Task<bool> RemoveIfEqualsAsync<T>(string key, T expected);
    Task<int> RemoveAllAsync<T>(IEnumerable<string>? keys = null);
    Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan? expiresIn = null);
    Task<bool> ReplaceIfEqualsAsync<T>(string key, T value, T expected, TimeSpan? expiresIn = null);
    Task<bool> ExistsAsync(string key);
    Task<TimeSpan?> GetExpirationAsync(string key);
    Task<bool> SetExpirationAsync(string key, TimeSpan expiresIn);
}