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
            await using var transaction = await db.Database.BeginTransactionAsync(context.RequestAborted);

            try
            {
                await next(context);

                // ÖNEMLİ: session id'yi ICorrelationContextAccessor'dan DEĞİL, HttpContext.Items'tan okuyoruz.
                // CorrelationContextAccessor'ın AsyncLocal holder'ı, next() içinde CompensatableApiActionFilter'ın
                // yeni bir session id set etmesi sırasında (holder'ı null'layıp yenisiyle değiştirme deseni,
                // bkz. CompensatableApiActionFilter.CompensationSessionIdItemsKey yorumu) bu middleware'in
                // (next()'i çağıran ancestor) hâlâ işaret ettiği ESKİ holder'ı null'lıyor — next() dönünce
                // CorrelationContext.CompensationSessionId burada HER ZAMAN null okunuyordu, event buffer
                // edilse bile asla dispatch edilemiyordu. HttpContext.Items bu indirection'dan etkilenmez.
                var compensationSessionId =
                    context.Items.TryGetValue(CompensatableApiActionFilter.CompensationSessionIdItemsKey, out var sessionIdObj) &&
                    sessionIdObj is Guid sid
                        ? sid
                        : (Guid?)null;

                logger.LogInformation(
                    "RequestTransactionMiddleware: post-next() CompensationSessionId={SessionId} for {Method} {Path}",
                    compensationSessionId,
                    context.Request.Method,
                    context.Request.Path);

                await transaction.CommitAsync(context.RequestAborted);

                // Dış transaction GERÇEKTEN commit olduktan sonra — PublishAfterCommitAsync ile
                // ertelenen event'ler ancak burada güvenle publish edilebilir. Dispatch hatası,
                // zaten commit edilmiş bir transaction'ı etkilememeli — sadece loglanır.
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
                            logger.LogInformation(
                                "RequestTransactionMiddleware: dispatching pending events after commit for compensation session {SessionId}",
                                sessionId);

                            // context.RequestAborted KULLANILMAZ: bu, client bağlantısına bağlı bir token —
                            // response client'a ulaşır ulaşmaz (özellikle uzun süren isteklerde) iptal
                            // olabilir. Dispatch, client hâlâ bağlı olsun ya da olmasın tamamlanmalı.
                            await compensationService.DispatchPendingEventsAsync(sessionId, CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to dispatch pending events after commit for compensation session {SessionId}", sessionId);
                    }
                }
                else
                {
                    logger.LogInformation(
                        "RequestTransactionMiddleware: no compensation session id after next() for {Method} {Path} — skipping pending-event dispatch",
                        context.Request.Method,
                        context.Request.Path);
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