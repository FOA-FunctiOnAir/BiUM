using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Interceptors;

public class BoltEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICorrelationContextProvider _correlationContextProvider;
    private readonly IDateTimeService _dateTimeService;

    public BoltEntitySaveChangesInterceptor(ICorrelationContextProvider correlationContextProvider, IDateTimeService dateTimeService)
    {
        _correlationContextProvider = correlationContextProvider;
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

    private void UpdateEntities(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var correlationContext = _correlationContextProvider.Get() ?? CorrelationContext.Empty;
        var now = _dateTimeService.Now.ToUniversalTime();

        foreach (var entry in dbContext.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CorrelationId = correlationContext.CorrelationId;
                if (entry.Entity is TenantBaseEntity tenantEntity && correlationContext.TenantId.HasValue)
                {
                    tenantEntity.TenantId = correlationContext.TenantId.Value;
                }
                entry.Entity.CreatedBy = correlationContext.User?.Id;
                entry.Entity.Created = DateOnly.FromDateTime(now);
                entry.Entity.CreatedTime = TimeOnly.FromDateTime(now);
            }
            else if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.CorrelationId = correlationContext.CorrelationId;
                if (entry.Entity is TenantBaseEntity tenantEntity && correlationContext.TenantId.HasValue)
                {
                    tenantEntity.TenantId = correlationContext.TenantId.Value;
                }
                entry.Entity.UpdatedBy = correlationContext.User?.Id;
                entry.Entity.Updated = DateOnly.FromDateTime(now);
                entry.Entity.UpdatedTime = TimeOnly.FromDateTime(now);
            }
        }
    }
}