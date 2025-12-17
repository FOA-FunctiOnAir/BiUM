using BiUM.Core.Models;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Interceptors;

public class BoltEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICorrelationContextProvider _correlationContextProvider;
    private readonly CorrelationContext _correlationContext;
    private readonly IDateTimeService _dateTimeService;

    public BoltEntitySaveChangesInterceptor(ICorrelationContextProvider correlationContextProvider, IDateTimeService dateTimeService)
    {
        _correlationContextProvider = correlationContextProvider;
        _dateTimeService = dateTimeService;

        _correlationContext = _correlationContextProvider.Get() ?? CorrelationContext.Empty;
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

        foreach (var entry in dbContext.ChangeTracker.Entries<BaseEntity>())
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
        };
    }
}