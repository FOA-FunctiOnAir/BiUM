using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.Compensation;

public interface ICompensationService
{
    public Task CommitSessionAsync(Guid compensationSessionId, CancellationToken cancellationToken);

    public Task RollbackSessionAsync(Guid compensationSessionId, CancellationToken cancellationToken);
}