using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.Compensation;

public interface ICompensationSessionFinalizedPublisher
{
    public Task PublishAsync(Guid compensationSessionId, bool success, CancellationToken cancellationToken = default);
}