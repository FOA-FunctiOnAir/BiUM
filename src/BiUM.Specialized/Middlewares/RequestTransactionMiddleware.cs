using BiUM.Core.Authorization;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services.Compensation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BiUM.Specialized.Middlewares;

public sealed class RequestTransactionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<RequestTransactionMiddleware> logger)
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

                // [CompensatableApi] varsa, session id'si next() sırasında ambient CorrelationContext'e
                // yazılmış olur ve burada hâlâ okunabilir (AsyncLocal). Commit'ten ÖNCE yakalıyoruz.
                var compensationSessionId = correlationContextAccessor.CorrelationContext?.CompensationSessionId;

                await transaction.CommitAsync(context.RequestAborted);

                // Dış transaction GERÇEKTEN commit olduktan sonra — PublishAfterCommitAsync ile
                // ertelenen event'ler ancak burada güvenle publish edilebilir. Dispatch hatası,
                // zaten commit edilmiş bir transaction'ı etkilememeli — sadece loglanır.
                if (compensationSessionId is { } sessionId && sessionId != Guid.Empty)
                {
                    try
                    {
                        var compensationService = context.RequestServices.GetService<ICompensationService>();

                        if (compensationService is not null)
                        {
                            await compensationService.DispatchPendingEventsAsync(sessionId, context.RequestAborted);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to dispatch pending events after commit for compensation session {SessionId}", sessionId);
                    }
                }
            }
            catch
            {
                await transaction.RollbackAsync(context.RequestAborted);

                throw;
            }
        });
    }
}