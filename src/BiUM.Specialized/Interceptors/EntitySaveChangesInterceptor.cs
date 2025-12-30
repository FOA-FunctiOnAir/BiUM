using BiUM.Core.Authorization;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Core.Models;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Common.Services;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Interceptors;

public class EntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICorrelationContextProvider _correlationContextProvider;
    private readonly CorrelationContext _correlationContext;
    private readonly IDateTimeService _dateTimeService;
    private readonly IRabbitMQClient? _rabbitMQClient;

    public EntitySaveChangesInterceptor(
        ICorrelationContextProvider correlationContextProvider,
        IDateTimeService dateTimeService,
        IRabbitMQClient? rabbitMQClient = null)
    {
        _correlationContextProvider = correlationContextProvider;
        _dateTimeService = dateTimeService;
        _rabbitMQClient = rabbitMQClient;

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

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await PublishAuditLogEventsAsync(eventData.Context);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
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
        }
    }

    private async Task PublishAuditLogEventsAsync(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        if (_rabbitMQClient is null)
        {
            return;
        }

        if (dbContext is not BaseDbContext baseDbContext)
        {
            return;
        }

        var userId = _correlationContext.User?.Id ?? Guid.Empty;

        foreach (var entry in baseDbContext.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
            {
                continue;
            }

            var entityType = entry.Entity.GetType();
            var entityName = entityType.Name;
            var entityId = entry.Entity.Id.ToString();

            string? beforeJson = null;
            string? afterJson = null;
            string? changedFieldsJson = null;
            var changeCount = 0;

            if (entry.State == EntityState.Added)
            {
                afterJson = JsonSerializer.Serialize(entry.Entity);
                changeCount = 1;
            }
            else if (entry.State == EntityState.Modified)
            {
                var changedProperties = entry.Properties
                    .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey())
                    .ToList();

                if (changedProperties.Any())
                {
                    var beforeValues = new Dictionary<string, object?>();
                    var afterValues = new Dictionary<string, object?>();
                    var changedFields = new List<string>();

                    foreach (var prop in changedProperties)
                    {
                        var propertyName = prop.Metadata.Name;
                        beforeValues[propertyName] = prop.OriginalValue;
                        afterValues[propertyName] = prop.CurrentValue;
                        changedFields.Add(propertyName);
                    }

                    beforeJson = JsonSerializer.Serialize(beforeValues);
                    afterJson = JsonSerializer.Serialize(afterValues);
                    changedFieldsJson = JsonSerializer.Serialize(changedFields);
                    changeCount = changedProperties.Count;
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                beforeJson = JsonSerializer.Serialize(entry.Entity);
                changeCount = 1;
            }

            if (changeCount > 0)
            {
                try
                {
                    var auditLogEvent = new AuditLogEvent
                    {
                        EntityName = entityName,
                        EntityId = entityId,
                        UserId = userId,
                        BeforeJson = beforeJson,
                        AfterJson = afterJson,
                        ChangedFieldsJson = changedFieldsJson,
                        ChangeCount = changeCount,
                        CorrelationContext = _correlationContext,
                        Created = DateOnly.FromDateTime(_dateTimeService.Now.ToUniversalTime()),
                        CreatedTime = TimeOnly.FromDateTime(_dateTimeService.Now.ToUniversalTime()),
                        CreatedBy = userId
                    };

                    await _rabbitMQClient.PublishAsync(auditLogEvent);
                }
                catch
                {
                }
            }
        }
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
