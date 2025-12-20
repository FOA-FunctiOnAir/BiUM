using BiUM.Core.Caching.Redis;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BiUM.Test.API.Controllers;

[BiUMRoute("test")]
public class TestRedisController : ApiControllerBase
{
    private readonly IRedisClient _redisClient;

    public TestRedisController(IRedisClient redisClient)
    {
        _redisClient = redisClient;
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        var item = await _redisClient.GetAsync<string>(key);

        if (!item.IsNull)
        {
            return Ok(new { Key = key, item.Value });
        }

        return NotFound($"Key '{key}' not found in cache.");
    }

    [HttpPost]
    public async Task<IActionResult> Set([FromBody] SetCacheRequest request)
    {
        await _redisClient.AddAsync(request.Key, request.Value, request.Expiration);

        return Ok($"Key '{request.Key}' set successfully.");
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        await _redisClient.RemoveAsync(key);

        return Ok($"Key '{key}' removed successfully.");
    }
}

public class SetCacheRequest
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public TimeSpan? Expiration { get; set; }
}