using BiUM.Core.Database;
using BiUM.Specialized.Middlewares;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public sealed class TransactionalUnitOfWorkRunner : ITransactionalUnitOfWorkRunner
{
    private readonly IDbContext? _db;

    public TransactionalUnitOfWorkRunner(IDbContext? db = null)
    {
        _db = db;
    }

    public async Task RunAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (_db is null || RequestTransactionMiddlewarePolicies.IsInMemoryDatabaseProvider(_db))
        {
            await action();

            return;
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            try
            {
                await action();

                var transaction = _db.Database.CurrentTransaction;

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    await transaction.DisposeAsync();
                }
            }
            catch
            {
                var transaction = _db.Database.CurrentTransaction;

                if (transaction is not null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    await transaction.DisposeAsync();
                }

                throw;
            }
        });
    }
}