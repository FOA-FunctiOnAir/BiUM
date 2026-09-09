using BiUM.Core.Authorization;
using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.Events;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.Compensation;

public sealed class CompensationSessionFinalizedHandler : IEventHandler<CompensationSessionFinalizedEvent>
{
    private readonly ICompensationService _compensationService;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public CompensationSessionFinalizedHandler(
        ICompensationService compensationService,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _compensationService = compensationService;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public async Task HandleAsync(CompensationSessionFinalizedEvent @event, CancellationToken cancellationToken = default)
    {
        var ctx = _correlationContextAccessor.CorrelationContext;

        if (ctx is not null)
        {
            _correlationContextAccessor.CorrelationContext = ctx.WithCompensationSessionId(@event.CompensationSessionId);
        }

        if (@event.Success)
        {
            await _compensationService.CommitSessionAsync(@event.CompensationSessionId, cancellationToken);
            await _compensationService.DispatchPendingEventsAsync(@event.CompensationSessionId, cancellationToken);

            return;
        }

        await _compensationService.RollbackSessionAsync(@event.CompensationSessionId, cancellationToken);
    }
}