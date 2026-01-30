using BiUM.Core.MessageBroker;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.External.Test.Events;

public class CurrencyEventsHandler : IEventHandler<CurrencyCreatedEvent>, IEventHandler<CurrencyUpdatedEvent>, IEventHandler<CurrencyDeletedEvent>
{
    private readonly ISender _mediator;
    private readonly ILogger<CurrencyEventsHandler> _logger;

    public CurrencyEventsHandler(
        ISender mediator,
        ILogger<CurrencyEventsHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public Task HandleAsync(CurrencyCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogWarning("{CurrencyName} currency created", @event.Name);

        return Task.CompletedTask;
    }

    public Task HandleAsync(CurrencyUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("{CurrencyName} currency updated", @event.Name);

        return Task.CompletedTask;
    }

    public Task HandleAsync(CurrencyDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("{CurrencyName} currency deleted", @event.Name);

        return Task.CompletedTask;
    }
}