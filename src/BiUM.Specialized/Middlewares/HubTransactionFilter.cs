using BiUM.Specialized.Database;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Middlewares;

public sealed class HubTransactionFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var db = invocationContext.ServiceProvider.GetService<IDbContext>();

        if (db is null || RequestTransactionMiddlewarePolicies.IsInMemoryDatabaseProvider(db))
        {
            return await next(invocationContext);
        }

        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                var result = await next(invocationContext);

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