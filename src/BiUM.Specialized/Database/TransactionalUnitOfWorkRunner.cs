using BiUM.Core.Database;
using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Specialized.Middlewares;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public sealed class TransactionalUnitOfWorkRunner : ITransactionalUnitOfWorkRunner
{
    private readonly IDbContext? _db;
    private readonly IRabbitMQClient? _rabbitMQClient;
    private readonly ILogger<TransactionalUnitOfWorkRunner> _logger;

    public TransactionalUnitOfWorkRunner(
        IDbContext? db = null,
        IRabbitMQClient? rabbitMQClient = null,
        ILogger<TransactionalUnitOfWorkRunner>? logger = null)
    {
        _db = db;
        _rabbitMQClient = rabbitMQClient;
        _logger = logger ?? NullLogger<TransactionalUnitOfWorkRunner>.Instance;
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
            var afterCommitScope = AfterCommitEventScope.BeginOwned();

            try
            {
                await action();

                var transaction = _db.Database.CurrentTransaction;

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    await transaction.DisposeAsync();
                }

                await AfterCommitEventDispatcher.FlushAsync(afterCommitScope, _rabbitMQClient, _logger);
            }
            catch
            {
                var transaction = _db.Database.CurrentTransaction;

                if (transaction is not null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    await transaction.DisposeAsync();
                }

                afterCommitScope?.Discard();

                throw;
            }
            finally
            {
                afterCommitScope?.End();
            }
        });
    }
}