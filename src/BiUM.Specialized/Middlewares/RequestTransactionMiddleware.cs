using BiUM.Specialized.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace BiUM.Specialized.Middlewares;

public sealed class RequestTransactionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (RequestTransactionMiddlewarePolicies.ShouldSkipTransaction(context))
        {
            await next(context);
            return;
        }

        var db = context.RequestServices.GetService<IDbContext>();

        if (db is null)
        {
            await next(context);

            return;
        }

        if (RequestTransactionMiddlewarePolicies.IsInMemoryDatabaseProvider(db))
        {
            await next(context);

            return;
        }

        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(context.RequestAborted);

            try
            {
                await next(context);
                await transaction.CommitAsync(context.RequestAborted);
            }
            catch
            {
                await transaction.RollbackAsync(context.RequestAborted);

                throw;
            }
        });
    }
}