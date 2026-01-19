using AutoMapper;
using BiUM.Contract.Models;
using BiUM.Core.Audit;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Common.Services;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
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
    private readonly IMapper? _mapper;
    private readonly BiAppOptions? _biAppOptions;

    private readonly List<IBaseEvent> _entityEventBuffer = [];
    private readonly List<AuditLogEvent> _auditBuffer = [];

    public EntitySaveChangesInterceptor(
        ICorrelationContextProvider correlationContextProvider,
        IDateTimeService dateTimeService,
        IRabbitMQClient? rabbitMQClient = null,
        IMapper? mapper = null,
        IOptions<BiAppOptions>? biAppOptions = null)
    {
        _correlationContextProvider = correlationContextProvider;
        _dateTimeService = dateTimeService;
        _rabbitMQClient = rabbitMQClient;
        _mapper = mapper;
        _biAppOptions = biAppOptions?.Value;

        _correlationContext = _correlationContextProvider.Get() ?? CorrelationContext.Empty;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        CollectAuditEntries(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        CollectAuditEntries(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        PublishEntityEventsAsync().GetAwaiter().GetResult();
        PublishAuditLogEventsAsync().GetAwaiter().GetResult();

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await PublishEntityEventsAsync();
        await PublishAuditLogEventsAsync();

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _auditBuffer.Clear();
        _entityEventBuffer.Clear();

        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _auditBuffer.Clear();
        _entityEventBuffer.Clear();

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void UpdateEntities(DbContext? dbContext)
    {
        if (dbContext is not BaseDbContext baseDbContext)
        {
            return;
        }

        var now = _dateTimeService.Now.ToUniversalTime();

        foreach (var entry in baseDbContext.ChangeTracker.Entries<IBaseEntity>())
        {
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
            else if (entry.State == EntityState.Deleted && !baseDbContext.HardDeleteEnabled)
            {
                entry.State = EntityState.Modified;
                entry.Entity.Deleted = true;

                entry.Entity.UpdatedBy = _correlationContext.User?.Id;
                entry.Entity.Updated = DateOnly.FromDateTime(now);
                entry.Entity.UpdatedTime = TimeOnly.FromDateTime(now);
            }

            CollectEntityEvents(entry);
        }
    }

    private void CollectEntityEvents(EntityEntry<IBaseEntity> entry)
    {
        IBaseEvent? baseEvent = null;

        if (entry.State == EntityState.Added)
        {
            baseEvent = entry.Entity.AddCreatedEvent(_mapper, null);
        }
        else if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
        {
            baseEvent = entry.Entity.AddUpdatedEvent(_mapper, null);
        }
        else if (entry.State == EntityState.Deleted)
        {
            baseEvent = entry.Entity.AddDeletedEvent(_mapper, null);
        }

        if (baseEvent is not null)
        {
            _entityEventBuffer.Add(baseEvent);
        }
    }

    private void CollectAuditEntries(DbContext? dbContext)
    {
        if (dbContext is not BaseDbContext baseDbContext)
        {
            return;
        }

        var userId = _correlationContext.User?.Id ?? Guid.Empty;

        foreach (var entry in baseDbContext.ChangeTracker.Entries<IBaseEntity>())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            if (!IsAuditable(entry.Entity))
            {
                continue;
            }

            var beforeJson = "{}";
            var afterJson = "{}";
            var changedFieldsJson = "{}";
            var changeCount = 0;

            switch (entry.State)
            {
                case EntityState.Added:
                    afterJson = JsonSerializer.Serialize(entry.Entity, entry.Entity.GetType());
                    changeCount = 1;

                    break;

                case EntityState.Modified:
                    {
                        var changedProps = entry.Properties
                            .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey())
                            .ToList();

                        if (changedProps.Count == 0)
                        {
                            continue;
                        }

                        var before = new Dictionary<string, object?>();
                        var fields = new List<string>();

                        foreach (var prop in changedProps)
                        {
                            before[prop.Metadata.Name] = prop.OriginalValue;

                            fields.Add(prop.Metadata.Name);
                        }

                        beforeJson = JsonSerializer.Serialize(before);
                        afterJson = JsonSerializer.Serialize(entry.Entity, entry.Entity.GetType());
                        changedFieldsJson = JsonSerializer.Serialize(fields);
                        changeCount = changedProps.Count;

                        break;
                    }

                case EntityState.Deleted:
                    beforeJson = JsonSerializer.Serialize(entry.Entity);
                    changeCount = 1;

                    break;
            }

            if (changeCount == 0)
            {
                continue;
            }

            _auditBuffer.Add(new AuditLogEvent
            {
                ServiceName = _biAppOptions?.Domain ?? string.Empty,
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id.ToString(),
                UserId = userId,
                BeforeJson = beforeJson,
                AfterJson = afterJson,
                ChangedFieldsJson = changedFieldsJson,
                ChangeCount = changeCount,
                Created = DateOnly.FromDateTime(_dateTimeService.Now.ToUniversalTime()),
                CreatedTime = TimeOnly.FromDateTime(_dateTimeService.Now.ToUniversalTime()),
                CreatedBy = userId
            });
        }
    }

    private async Task PublishEntityEventsAsync()
    {
        if (_rabbitMQClient is null || _entityEventBuffer.Count == 0)
        {
            return;
        }

        foreach (var evt in _entityEventBuffer)
        {
            await _rabbitMQClient.PublishAsync(evt);
        }

        _entityEventBuffer.Clear();
    }

    private async Task PublishAuditLogEventsAsync()
    {
        if (_rabbitMQClient is null || _auditBuffer.Count == 0)
        {
            return;
        }

        foreach (var auditEvent in _auditBuffer)
        {
            await _rabbitMQClient.PublishAsync(auditEvent);
        }

        _auditBuffer.Clear();
    }

    private static bool IsAuditable(object entity)
    {
        var attr = entity.GetType()
            .GetCustomAttributes(typeof(AuditableAttribute), false)
            .FirstOrDefault() as AuditableAttribute;

        return attr?.Enabled != false;
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry)
    {
        return entry.References.Any(r =>
            r.TargetEntry is not null &&
            r.TargetEntry.Metadata.IsOwned() &&
            r.TargetEntry.State == EntityState.Modified);
    }
}
