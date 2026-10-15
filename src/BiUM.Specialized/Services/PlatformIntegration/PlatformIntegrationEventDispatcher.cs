using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.PlatformIntegration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.PlatformIntegration;

public abstract class PlatformIntegrationEventDispatcher : IEventHandler<PlatformIntegrationEvent>
{
    private readonly IEnumerable<IPlatformEventHandler> _handlers;
    private readonly ILogger _logger;

    protected PlatformIntegrationEventDispatcher(IEnumerable<IPlatformEventHandler> handlers, ILogger logger)
    {
        _handlers = handlers;
        _logger = logger;
    }

    public async Task HandleAsync(PlatformIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var handlers = _handlers
            .Where(handler => string.Equals(handler.EventCode, @event.EventCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (handlers.Count == 0)
        {
            _logger.LogDebug("No platform event handler registered for {EventCode}", @event.EventCode);

            return;
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }
}