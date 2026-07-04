using BiUM.Contract.Models.Caching.Redis;
using BiUM.Core.Caching.InMemory;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.Caching.InMemory;

public class InMemoryClient : IInMemoryClient
{
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConcurrentDictionary<string, DateTimeOffset?> _keyRegistry = new();
    private readonly SemaphoreSlim _atomicLock = new(1, 1);

    public InMemoryClient(IMemoryCache cache, JsonSerializerOptions jsonOptions)
    {
        _cache = cache;
        _jsonOptions = jsonOptions;
    }

    public Task<CacheItem<T>> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue<T>(key, out var value) && value is not null)
        {
            return Task.FromResult(new CacheItem<T>(value, true));
        }

        return Task.FromResult(CacheItem<T>.NoValue);
    }

    public async Task<IDictionary<string, CacheItem<T>>> GetAllAsync<T>(IEnumerable<string> keys)
    {
        var result = new Dictionary<string, CacheItem<T>>();

        foreach (var key in keys)
        {
            result[key] = await GetAsync<T>(key);
        }

        return result;
    }

    public Task<bool> AddAsync<T>(string key, T value, TimeSpan? expiresIn = null)
    {
        if (key is null || value is null)
        {
            return Task.FromResult(false);
        }

        var options = new MemoryCacheEntryOptions();

        if (expiresIn.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiresIn;
        }

        options.RegisterPostEvictionCallback((k, _, _, _) =>
            _keyRegistry.TryRemove(k.ToString()!, out _));

        _cache.Set(key, value, options);
        _keyRegistry[key] = expiresIn.HasValue ? DateTimeOffset.UtcNow.Add(expiresIn.Value) : null;

        return Task.FromResult(true);
    }

    public Task<bool> RemoveAsync(string key)
    {
        _cache.Remove(key);
        _keyRegistry.TryRemove(key, out _);

        return Task.FromResult(true);
    }

    public async Task<bool> RemoveIfEqualsAsync<T>(string key, T expected)
    {
        await _atomicLock.WaitAsync();

        try
        {
            if (!_cache.TryGetValue<T>(key, out var current))
            {
                return false;
            }

            if (JsonSerializer.Serialize(current, _jsonOptions) != JsonSerializer.Serialize(expected, _jsonOptions))
            {
                return false;
            }

            await RemoveAsync(key);

            return true;
        }
        finally
        {
            _atomicLock.Release();
        }
    }

    public Task<int> RemoveAllAsync<T>(IEnumerable<string>? keys = null)
    {
        if (keys is null)
        {
            var allKeys = _keyRegistry.Keys.ToList();

            foreach (var k in allKeys)
            {
                _cache.Remove(k);
                _keyRegistry.TryRemove(k, out _);
            }

            return Task.FromResult(allKeys.Count);
        }

        var keyList = keys.Where(k => !string.IsNullOrEmpty(k)).ToList();

        foreach (var k in keyList)
        {
            _cache.Remove(k);
            _keyRegistry.TryRemove(k, out _);
        }

        return Task.FromResult(keyList.Count);
    }

    public Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan? expiresIn = null)
    {
        return AddAsync(key, value, expiresIn);
    }

    public async Task<bool> ReplaceIfEqualsAsync<T>(string key, T value, T expected, TimeSpan? expiresIn = null)
    {
        await _atomicLock.WaitAsync();

        try
        {
            if (!_cache.TryGetValue<T>(key, out var current))
            {
                return false;
            }

            if (JsonSerializer.Serialize(current, _jsonOptions) != JsonSerializer.Serialize(expected, _jsonOptions))
            {
                return false;
            }

            await AddAsync(key, value, expiresIn);

            return true;
        }
        finally
        {
            _atomicLock.Release();
        }
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }

    public Task<TimeSpan?> GetExpirationAsync(string key)
    {
        if (!_keyRegistry.TryGetValue(key, out var expiry))
        {
            return Task.FromResult<TimeSpan?>(null);
        }

        if (!expiry.HasValue)
        {
            return Task.FromResult<TimeSpan?>(null);
        }

        var remaining = expiry.Value - DateTimeOffset.UtcNow;

        return Task.FromResult<TimeSpan?>(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
    }

    public Task<bool> SetExpirationAsync(string key, TimeSpan expiresIn)
    {
        if (!_cache.TryGetValue<object>(key, out var rawValue) || rawValue is null)
        {
            return Task.FromResult(false);
        }

        _keyRegistry.TryRemove(key, out _);

        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiresIn };

        options.RegisterPostEvictionCallback((k, _, _, _) =>
            _keyRegistry.TryRemove(k.ToString()!, out _));

        _cache.Set(key, rawValue, options);
        _keyRegistry[key] = DateTimeOffset.UtcNow.Add(expiresIn);

        return Task.FromResult(true);
    }

    public Task<string?> GetRawAsync(string key)
    {
        if (_cache.TryGetValue<object>(key, out var value) && value is not null)
        {
            return Task.FromResult<string?>(System.Text.Json.JsonSerializer.Serialize(value, _jsonOptions));
        }

        return Task.FromResult<string?>(null);
    }

    public Task<IEnumerable<string>> ScanKeysAsync(string pattern = "*")
    {
        var regex = GlobToRegex(pattern);

        var matched = _keyRegistry.Keys
            .Where(k => regex.IsMatch(k) && _cache.TryGetValue(k, out _))
            .ToList();

        return Task.FromResult<IEnumerable<string>>(matched);
    }

    private static Regex GlobToRegex(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}