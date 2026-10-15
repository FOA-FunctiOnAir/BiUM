using BiUM.Core.Caching.Redis;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.PlatformIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Features.PlatformEvents;

public class SamplePlatformEventHandler : IPlatformEventHandler
{
    public const string Code = "test2-sample-event";

    public const string LastEventCacheKey = "bium:sample:test2:platform-event:last";

    private readonly IRedisClient? _redisClient;

    public SamplePlatformEventHandler(IEnumerable<IRedisClient> redisClients)
    {
        _redisClient = redisClients.FirstOrDefault();
    }

    public string EventCode => Code;

    public async Task HandleAsync(PlatformIntegrationEvent @event, CancellationToken cancellationToken)
    {
        if (_redisClient is null)
        {
            return;
        }

        await _redisClient.AddAsync(LastEventCacheKey, new SamplePlatformEventReceipt(@event.EventCode, @event.Parameters, DateTime.UtcNow), TimeSpan.FromHours(1));
    }
}

public sealed record SamplePlatformEventReceipt(string? EventCode, IReadOnlyDictionary<string, string?> Parameters, DateTime ReceivedAt);