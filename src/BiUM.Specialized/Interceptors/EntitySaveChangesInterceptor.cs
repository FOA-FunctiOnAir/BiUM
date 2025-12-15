using BiUM.Core.Models;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BiUM.Specialized.Interceptors;

public class EntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICorrelationContextProvider _correlationContextProvider;
    private readonly CorrelationContext _correlationContext;
    private readonly IDateTimeService _dateTimeService;

    public EntitySaveChangesInterceptor(ICorrelationContextProvider correlationContextProvider, IDateTimeService dateTimeService)
    {
        _correlationContextProvider = correlationContextProvider;
        _dateTimeService = dateTimeService;

        _correlationContext = _correlationContextProvider.Get();
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? dbContext)
    {
        if (dbContext is null) return;

        if (dbContext is not BaseDbContext baseDbContext) return;

        foreach (var entry in baseDbContext.ChangeTracker.Entries<BaseEntity>())
        {
            var now = _dateTimeService.Now.ToUniversalTime();

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CorrelationId = _correlationContext.CorrelationId;
                entry.Entity.CreatedBy = _correlationContext.User?.Id;
                entry.Entity.Created = DateOnly.FromDateTime(now);
                entry.Entity.CreatedTime = TimeOnly.FromDateTime(now);
            }
            else if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.UpdatedBy = _correlationContext.User?.Id;
                entry.Entity.Updated = DateOnly.FromDateTime(now);
                entry.Entity.UpdatedTime = TimeOnly.FromDateTime(now);
            }
            else if (entry.State == EntityState.Deleted)
            {
                if (!baseDbContext.HardDelete)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.Deleted = true;

                    entry.Entity.UpdatedBy = _correlationContext.User?.Id;
                    entry.Entity.Updated = DateOnly.FromDateTime(now);
                    entry.Entity.UpdatedTime = TimeOnly.FromDateTime(now);
                }
            }
        };
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry is not null &&
            r.TargetEntry.Metadata.IsOwned() &&
            (r.TargetEntry.State == EntityState.Modified));
}