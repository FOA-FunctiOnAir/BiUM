using BiUM.Core.Caching.Redis;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BiApp.Test2.API.Controllers;


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

    [HttpPost("{key}")]
    public async Task<IActionResult> AcquireLock(string key, [FromQuery] int seconds = 30)
    {
        var token = Guid.NewGuid();

        var acquired = await _redisClient.AddIfNotExistsAsync(key, token, TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 300)));

        return acquired ? Ok(new { Key = key, Token = token }) : Conflict($"Key '{key}' is already locked.");
    }

    [HttpDelete("{key}/{token:guid}")]
    public async Task<IActionResult> ReleaseLock(string key, Guid token)
    {
        var released = await _redisClient.RemoveIfEqualsAsync(key, token);

        return released ? Ok($"Lock '{key}' released.") : Conflict($"Lock '{key}' is not held by token {token}.");
    }
}

public class SetCacheRequest
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public TimeSpan? Expiration { get; set; }
}