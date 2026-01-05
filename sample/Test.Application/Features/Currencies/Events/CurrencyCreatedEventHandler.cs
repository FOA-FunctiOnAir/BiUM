using BiUM.Core.MessageBroker;
using BiUM.Test.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test.Application.Features.Currencies.Events;

public partial class CurrencyCreatedEventHandler : IEventHandler<CurrencyCreatedEvent>
{
    private readonly ISender _mediator;
    private readonly ILogger<CurrencyCreatedEventHandler> _logger;

    public CurrencyCreatedEventHandler(
        ISender mediator,
        ILogger<CurrencyCreatedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(CurrencyCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogWarning("CurrencyCreatedEvent triggered for {Name}", @event.Name);
    }
}
