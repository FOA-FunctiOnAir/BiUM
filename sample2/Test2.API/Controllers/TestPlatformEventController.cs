using BiApp.Test2.Application.Features.PlatformEvents;
using BiUM.Core.Caching.Redis;
using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.API.Controllers;

[BiUMRoute("test")]
public class TestPlatformEventController : ApiControllerBase
{
    private readonly IRabbitMQClient _rabbitMQClient;
    private readonly IRedisClient _redisClient;
    private readonly BiAppOptions _biAppOptions;

    public TestPlatformEventController(IRabbitMQClient rabbitMQClient, IRedisClient redisClient, IOptions<BiAppOptions> biAppOptions)
    {
        _rabbitMQClient = rabbitMQClient;
        _redisClient = redisClient;
        _biAppOptions = biAppOptions.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] Dictionary<string, string?>? parameters, [FromQuery] string? eventCode, CancellationToken cancellationToken)
    {
        var platformEvent = new PlatformIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            EventCode = eventCode ?? SamplePlatformEventHandler.Code
        };

        foreach (var parameter in parameters ?? [])
        {
            platformEvent.Parameters[parameter.Key] = parameter.Value;
        }

        await _rabbitMQClient.PublishToDomainAsync(_biAppOptions.Domain, platformEvent, cancellationToken);

        return Ok(new { platformEvent.EventId, platformEvent.EventCode });
    }

    [HttpGet]
    public async Task<IActionResult> GetLastReceived()
    {
        var receipt = await _redisClient.GetAsync<SamplePlatformEventReceipt>(SamplePlatformEventHandler.LastEventCacheKey);

        return receipt.Value is null ? NotFound() : Ok(receipt.Value);
    }
}