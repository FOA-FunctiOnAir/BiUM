using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Middlewares;

public static class DbTransactionRunner
{
    public static async Task<TResult> ExecuteAsync<TResult>(IDbContext db, Func<Task<TResult>> operation)
    {
        if (RequestTransactionMiddlewarePolicies.IsInMemoryDatabaseProvider(db))
        {
            return await operation();
        }

        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                var result = await operation();

                var transaction = db.Database.CurrentTransaction;

                if (transaction is not null)
                {
                    await transaction.CommitAsync(CancellationToken.None);
                    await transaction.DisposeAsync();
                }

                return result;
            }
            catch
            {
                var transaction = db.Database.CurrentTransaction;

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