using BiUM.Contract.Models.Api;
using BiUM.Core.Caching.InMemory;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

[ApiController]
[BiUMBaseRoute]
public class InMemoryCacheController : ApiControllerBase
{
    private readonly IInMemoryClient? _inMemoryClient;

    public InMemoryCacheController(IEnumerable<IInMemoryClient> inMemoryClients)
    {
        _inMemoryClient = inMemoryClients.FirstOrDefault();
    }

    [HttpGet]
    public async Task<ApiResponse<IList<InMemoryCacheKeyDto>>> GetCacheKeys([FromQuery] string? pattern = null)
    {
        if (_inMemoryClient is null)
        {
            return new ApiResponse<IList<InMemoryCacheKeyDto>>([]);
        }

        var searchPattern = string.IsNullOrWhiteSpace(pattern) ? "*" : pattern;
        var keys = (await _inMemoryClient.ScanKeysAsync(searchPattern)).ToList();

        if (keys.Count == 0)
        {
            return new ApiResponse<IList<InMemoryCacheKeyDto>>([]);
        }

        var ttlTasks = keys.Select(k => _inMemoryClient.GetExpirationAsync(k)).ToList();
        var ttls = await Task.WhenAll(ttlTasks);

        var result = keys
            .Select((k, i) => new InMemoryCacheKeyDto { Key = k, Ttl = ttls[i] })
            .ToList();

        return new ApiResponse<IList<InMemoryCacheKeyDto>>(result);
    }

    [HttpGet]
    public async Task<ApiResponse<InMemoryCacheKeyValueDto>> GetCacheKeyValue([FromQuery] string key)
    {
        if (_inMemoryClient is null || string.IsNullOrWhiteSpace(key))
        {
            return new ApiResponse<InMemoryCacheKeyValueDto>(new InMemoryCacheKeyValueDto { Key = key });
        }

        var rawValue = await _inMemoryClient.GetRawAsync(key);
        var ttl = await _inMemoryClient.GetExpirationAsync(key);

        return new ApiResponse<InMemoryCacheKeyValueDto>(new InMemoryCacheKeyValueDto
        {
            Key = key,
            Value = rawValue,
            Ttl = ttl
        });
    }

    [HttpDelete]
    public async Task<ApiResponse<int>> DeleteCacheKeys([FromBody] InMemoryDeleteCacheKeysRequest request)
    {
        if (_inMemoryClient is null)
        {
            return new ApiResponse<int>(0);
        }

        var keys = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.Key))
        {
            keys.Add(request.Key);
        }

        if (request.Keys is { Count: > 0 })
        {
            keys.AddRange(request.Keys);
        }

        if (keys.Count == 0)
        {
            return new ApiResponse<int>(0);
        }

        var count = await _inMemoryClient.RemoveAllAsync<object>(keys.Distinct());

        return new ApiResponse<int>(count);
    }

    [HttpDelete]
    public async Task<ApiResponse<int>> DeleteCacheKeysByPattern([FromBody] InMemoryDeleteCacheKeysByPatternRequest request)
    {
        if (_inMemoryClient is null || string.IsNullOrWhiteSpace(request.Pattern))
        {
            return new ApiResponse<int>(0);
        }

        var keys = (await _inMemoryClient.ScanKeysAsync(request.Pattern)).ToList();

        if (keys.Count == 0)
        {
            return new ApiResponse<int>(0);
        }

        var count = await _inMemoryClient.RemoveAllAsync<object>(keys);

        return new ApiResponse<int>(count);
    }

    [HttpDelete]
    public async Task<ApiResponse<int>> DeleteAllCacheKeys()
    {
        if (_inMemoryClient is null)
        {
            return new ApiResponse<int>(0);
        }

        var count = await _inMemoryClient.RemoveAllAsync<object>();

        return new ApiResponse<int>(count);
    }
}

public class InMemoryCacheKeyDto
{
    public string Key { get; set; } = string.Empty;
    public TimeSpan? Ttl { get; set; }
}

public class InMemoryCacheKeyValueDto
{
    public string? Key { get; set; }
    public string? Value { get; set; }
    public TimeSpan? Ttl { get; set; }
}

public class InMemoryDeleteCacheKeysRequest
{
    public string? Key { get; set; }
    public IList<string>? Keys { get; set; }
}

public class InMemoryDeleteCacheKeysByPatternRequest
{
    public string Pattern { get; set; } = string.Empty;
}