using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BiUM.Infrastructure.Common.Interceptors;

public class EntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public EntitySaveChangesInterceptor(ICurrentUserService currentUserService, IDateTimeService dateTimeService)
    {
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
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

    private void UpdateEntities(DbContext? AuthDbContext)
    {
        if (AuthDbContext is null) return;

        Parallel.ForEach(AuthDbContext.ChangeTracker.Entries<BaseEntity>(), entry =>
        {
            var now = _dateTimeService.Now.ToUniversalTime();

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CorrelationId = _currentUserService.CorrelationId;
                entry.Entity.CreatedBy = _currentUserService.UserId;
                entry.Entity.Created = DateOnly.FromDateTime(now);
                entry.Entity.CreatedTime = TimeOnly.FromDateTime(now);
            }
            else if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.UpdatedBy = _currentUserService.UserId;
                entry.Entity.Updated = DateOnly.FromDateTime(now);
                entry.Entity.UpdatedTime = TimeOnly.FromDateTime(now);
            }
        });
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry != null &&
            r.TargetEntry.Metadata.IsOwned() &&
            (r.TargetEntry.State == EntityState.Modified));
}