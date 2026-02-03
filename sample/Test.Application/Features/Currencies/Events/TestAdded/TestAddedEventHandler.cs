using BiUM.Core.MessageBroker;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Application.Features.Currencies.Events.TestAdded;

public class TestAddedEventHandler : IEventHandler<TestAddedEvent>
{
    private readonly ISender _mediator;
    private readonly ILogger<TestAddedEventHandler> _logger;

    public TestAddedEventHandler(
        ISender mediator,
        ILogger<TestAddedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(TestAddedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogWarning("TestAddedEvent triggered for {EventKey}", @event.Key);
    }
}