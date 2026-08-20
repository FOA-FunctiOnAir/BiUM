using BiUM.Core.Compensation;
using BiUM.Core.Database;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services.Compensation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Middlewares;

public sealed class RequestTransactionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
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
            try
            {
                await next(context);

                var transaction = db.Database.CurrentTransaction;

                var rollbackRequested =
                    context.Items.TryGetValue(ApiResponseTransactionRollbackFilter.RollbackRequestedKey, out var rollbackFlag) &&
                    rollbackFlag is true;

                if (rollbackRequested)
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        await transaction.DisposeAsync();
                    }

                    await PublishPendingCompensationFinalizedEventAsync(context, logger);

                    return;
                }

                var compensationSessionId =
                    context.Items.TryGetValue(CompensatableApiActionFilter.CompensationSessionIdItemsKey, out var sessionIdObj) &&
                    sessionIdObj is Guid sid
                        ? sid
                        : (Guid?)null;

                if (transaction is not null)
                {
                    await transaction.CommitAsync(context.RequestAborted);
                    await transaction.DisposeAsync();
                }

                await PublishPendingCompensationFinalizedEventAsync(context, logger);

                if (compensationSessionId is { } sessionId && sessionId != Guid.Empty)
                {
                    try
                    {
                        var compensationService = context.RequestServices.GetService<ICompensationService>();

                        if (compensationService is null)
                        {
                            logger.LogWarning(
                                "Cannot dispatch pending events after commit for compensation session {SessionId}: ICompensationService is not resolvable from RequestServices.",
                                sessionId);
                        }
                        else
                        {
                            var unitOfWorkRunner = context.RequestServices.GetService<ITransactionalUnitOfWorkRunner>();

                            if (unitOfWorkRunner is not null)
                            {
                                await unitOfWorkRunner.RunAsync(
                                    () => compensationService.DispatchPendingEventsAsync(sessionId, CancellationToken.None),
                                    CancellationToken.None);
                            }
                            else
                            {
                                await compensationService.DispatchPendingEventsAsync(sessionId, CancellationToken.None);
                            }
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
                var transaction = db.Database.CurrentTransaction;

                if (transaction is not null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    await transaction.DisposeAsync();
                }

                await PublishPendingCompensationFinalizedEventAsync(context, logger);

                throw;
            }
        });
    }

    private static async Task PublishPendingCompensationFinalizedEventAsync(HttpContext context, ILogger logger)
    {
        if (!context.Items.TryGetValue(CompensatableApiActionFilter.PendingFinalizeEventKey, out var pending) ||
            pending is not (Guid sessionId, bool success))
        {
            return;
        }

        context.Items.Remove(CompensatableApiActionFilter.PendingFinalizeEventKey);

        try
        {
            var publisher = context.RequestServices.GetService<ICompensationSessionFinalizedPublisher>();

            if (publisher is null)
            {
                logger.LogWarning(
                    "Cannot publish CompensationSessionFinalizedEvent for session {SessionId}: ICompensationSessionFinalizedPublisher is not resolvable from RequestServices.",
                    sessionId);

                return;
            }

            await publisher.PublishAsync(sessionId, success, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish CompensationSessionFinalizedEvent for session {SessionId}", sessionId);
        }
    }
}