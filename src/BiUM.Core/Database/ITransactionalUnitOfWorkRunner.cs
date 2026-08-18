using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.Database;

public interface ITransactionalUnitOfWorkRunner
{
    Task RunAsync(Func<Task> action, CancellationToken cancellationToken = default);
}