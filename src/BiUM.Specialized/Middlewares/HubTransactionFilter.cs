using BiUM.Specialized.Database;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BiUM.Specialized.Middlewares;

public sealed class HubTransactionFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var db = invocationContext.ServiceProvider.GetService<IDbContext>();

        if (db is null)
        {
            return await next(invocationContext);
        }

        return await DbTransactionRunner.ExecuteAsync(db, () => next(invocationContext).AsTask());
    }
}