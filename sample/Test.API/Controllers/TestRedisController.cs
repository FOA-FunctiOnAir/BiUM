using BiUM.Core.Caching.Redis;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BiApp.Test.API.Controllers;

[BiUMRoute("test")]
public class TestRedisController : ApiControllerBase
{
    private const string GatewayClientKey = "Gateway";

    private readonly IRedisClient _redisClient;

    public TestRedisController(IRedisClient redisClient)
    {
        _redisClient = redisClient;
    }

    [HttpGet("{key}")]
    public Task<IActionResult> Get(string key) => GetCore(_redisClient, key);

    [HttpPost]
    public Task<IActionResult> Set([FromBody] SetCacheRequest request) => SetCore(_redisClient, request);

    [HttpDelete("{key}")]
    public Task<IActionResult> Delete(string key) => DeleteCore(_redisClient, key);

    [HttpGet("Gateway/{key}")]
    public Task<IActionResult> GetGateway(
        string key,
        [FromKeyedServices(GatewayClientKey)] IRedisClient gatewayClient) =>
        GetCore(gatewayClient, key);

    [HttpPost("Gateway")]
    public Task<IActionResult> SetGateway(
        [FromBody] SetCacheRequest request,
        [FromKeyedServices(GatewayClientKey)] IRedisClient gatewayClient) =>
        SetCore(gatewayClient, request);

    [HttpDelete("Gateway/{key}")]
    public Task<IActionResult> DeleteGateway(
        string key,
        [FromKeyedServices(GatewayClientKey)] IRedisClient gatewayClient) =>
        DeleteCore(gatewayClient, key);

    private static async Task<IActionResult> GetCore(IRedisClient client, string key)
    {
        var item = await client.GetAsync<string>(key);

        if (!item.IsNull)
        {
            return new OkObjectResult(new { Key = key, item.Value });
        }

        return new NotFoundObjectResult($"Key '{key}' not found in cache.");
    }

    private static async Task<IActionResult> SetCore(IRedisClient client, SetCacheRequest request)
    {
        await client.AddAsync(request.Key, request.Value, request.Expiration);
        return new OkObjectResult($"Key '{request.Key}' set successfully.");
    }

    private static async Task<IActionResult> DeleteCore(IRedisClient client, string key)
    {
        await client.RemoveAsync(key);
        return new OkObjectResult($"Key '{key}' removed successfully.");
    }
}

public class SetCacheRequest
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public TimeSpan? Expiration { get; set; }
}