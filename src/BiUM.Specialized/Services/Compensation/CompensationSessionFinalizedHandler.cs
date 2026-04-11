using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.Events;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.Compensation;

public sealed class CompensationSessionFinalizedHandler : IEventHandler<CompensationSessionFinalizedEvent>
{
    private readonly ICompensationService _compensationService;

    public CompensationSessionFinalizedHandler(ICompensationService compensationService)
    {
        _compensationService = compensationService;
    }

    public Task HandleAsync(CompensationSessionFinalizedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event.Success)
        {
            return _compensationService.CommitSessionAsync(@event.CompensationSessionId, cancellationToken);
        }

        return _compensationService.RollbackSessionAsync(@event.CompensationSessionId, cancellationToken);
    }
}
