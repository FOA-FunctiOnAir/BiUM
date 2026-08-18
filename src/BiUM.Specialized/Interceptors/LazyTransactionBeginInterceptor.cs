using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Interceptors;

public sealed class LazyTransactionBeginInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        BeginIfNeeded(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await BeginIfNeededAsync(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void BeginIfNeeded(DbContext? context)
    {
        if (context is null || context.Database.CurrentTransaction is not null || !context.Database.IsRelational())
        {
            return;
        }

        context.Database.BeginTransaction();
    }

    private static async Task BeginIfNeededAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null || context.Database.CurrentTransaction is not null || !context.Database.IsRelational())
        {
            return;
        }

        await context.Database.BeginTransactionAsync(cancellationToken);
    }
}