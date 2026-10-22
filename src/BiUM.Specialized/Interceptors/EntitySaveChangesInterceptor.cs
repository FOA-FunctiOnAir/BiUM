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
using BiUM.Specialized.Compensation;
using BiUM.Specialized.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Interceptors;

public class EntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICorrelationContextProvider _correlationContextProvider;
    private readonly IDateTimeService _dateTimeService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQClient? _rabbitMQClient;
    private readonly IMapper? _mapper;
    private readonly BiAppOptions? _biAppOptions;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly EntityChangeEventOptions _entityChangeEventOptions;
    private readonly ILogger<EntitySaveChangesInterceptor> _logger;

    private readonly List<BaseEvent> _entityEventBuffer = [];
    private readonly List<AuditLogEvent> _auditBuffer = [];
    private readonly List<(string Table, string Operation, Guid Id)> _entityChangeBuffer = [];

    private static readonly HashSet<string> DefaultExcludedChangeTables = new(StringComparer.OrdinalIgnoreCase) { "__PENDING_EVENT" };

    // IsAuditable is cached per Type with a 5-minute TTL (attribute values never change at runtime,
    // but TTL allows hot-reload scenarios without requiring a restart).
    private static readonly ConcurrentDictionary<Type, (bool Value, long ExpiresAtTicks)> _auditableCache = new();
    private static readonly long _auditableCacheTtlTicks = TimeSpan.FromHours(1).Ticks;

    public EntitySaveChangesInterceptor(
        ICorrelationContextProvider correlationContextProvider,
        IDateTimeService dateTimeService,
        IServiceProvider serviceProvider,
        IRabbitMQClient? rabbitMQClient = null,
        IMapper? mapper = null,
        IOptions<BiAppOptions>? biAppOptions = null,
        IHttpContextAccessor? httpContextAccessor = null,
        ILogger<EntitySaveChangesInterceptor>? logger = null,
        IOptions<EntityChangeEventOptions>? entityChangeEventOptions = null)
    {
        _correlationContextProvider = correlationContextProvider;
        _dateTimeService = dateTimeService;
        _serviceProvider = serviceProvider;
        _rabbitMQClient = rabbitMQClient;
        _mapper = mapper;
        _biAppOptions = biAppOptions?.Value;
        _httpContextAccessor = httpContextAccessor;
        _entityChangeEventOptions = entityChangeEventOptions?.Value ?? new EntityChangeEventOptions();
        _logger = logger ?? NullLogger<EntitySaveChangesInterceptor>.Instance;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EnsureNotSavingOnHttpGet(eventData.Context);
        CollectTranslationChanges(eventData.Context);

        // L-6: compute ChangeTracker entries once and pass to both methods.
        if (eventData.Context is BaseDbContext bdc)
        {
            var entries = bdc.ChangeTracker.Entries<IBaseEntity>().ToList();
            UpdateEntities(bdc, entries);
            CompensationEntityProcessor.Apply(bdc, _correlationContextProvider, _serviceProvider, _dateTimeService);
            CollectAuditEntries(bdc, entries);
        }
        else
        {
            UpdateEntities(eventData.Context);
            CompensationEntityProcessor.Apply(eventData.Context!, _correlationContextProvider, _serviceProvider, _dateTimeService);
            CollectAuditEntries(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnsureNotSavingOnHttpGet(eventData.Context);
        CollectTranslationChanges(eventData.Context);

        // L-6 + M-3: single ChangeTracker scan; async path uses ApplyAsync (no sync-over-async).
        if (eventData.Context is BaseDbContext bdc)
        {
            var entries = bdc.ChangeTracker.Entries<IBaseEntity>().ToList();
            UpdateEntities(bdc, entries);
            await CompensationEntityProcessor.ApplyAsync(bdc, _correlationContextProvider, _serviceProvider, _dateTimeService, cancellationToken);
            CollectAuditEntries(bdc, entries);
        }
        else
        {
            UpdateEntities(eventData.Context);
            await CompensationEntityProcessor.ApplyAsync(eventData.Context!, _correlationContextProvider, _serviceProvider, _dateTimeService, cancellationToken);
            CollectAuditEntries(eventData.Context);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        DispatchBufferedEvents();

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        DispatchBufferedEvents();

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        ClearBuffers();

        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ClearBuffers();

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void ClearBuffers()
    {
        _auditBuffer.Clear();
        _entityEventBuffer.Clear();
        _entityChangeBuffer.Clear();
    }

    private void DispatchBufferedEvents()
    {
        var afterCommitScope = AfterCommitEventScope.Current;

        if (afterCommitScope is null)
        {
            var entityChangesEvent = BuildEntityChangesEvent(_entityChangeBuffer);

            _entityChangeBuffer.Clear();

            _ = SafePublishAsync(PublishEntityEventsAsync());
            _ = SafePublishAsync(PublishAuditLogEventsAsync());

            if (entityChangesEvent is not null && _rabbitMQClient is not null)
            {
                _ = SafePublishAsync(_rabbitMQClient.PublishAsync(entityChangesEvent));
            }

            return;
        }

        foreach (var entityEvent in _entityEventBuffer)
        {
            afterCommitScope.Enqueue(entityEvent);
        }

        foreach (var auditEvent in _auditBuffer)
        {
            afterCommitScope.Enqueue(auditEvent);
        }

        if (_entityChangeBuffer.Count > 0)
        {
            var committedEvent = afterCommitScope.GetOrAddState(() =>
            {
                var created = CreateEntityChangesEvent();

                afterCommitScope.Enqueue(created);

                return created;
            });

            AppendEntityChanges(committedEvent, _entityChangeBuffer);
        }

        ClearBuffers();
    }

    private void CollectTranslationChanges(DbContext? dbContext)
    {
        if (dbContext is null || !_entityChangeEventOptions.Enable)
        {
            return;
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<ITranslationBaseEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var table = entry.Metadata.GetTableName();

            if (!IsChangeTrackedTable(table) || entry.Entity.RecordId == Guid.Empty)
            {
                continue;
            }

            _entityChangeBuffer.Add((table!, EntityChangeOperations.Updated, entry.Entity.RecordId));
        }
    }

    private bool IsChangeTrackedTable(string? table) =>
        !string.IsNullOrWhiteSpace(table) &&
        !DefaultExcludedChangeTables.Contains(table) &&
        !_entityChangeEventOptions.ExcludedTables.Contains(table, StringComparer.OrdinalIgnoreCase);

    private void CollectEntityChange(EntityEntry<IBaseEntity> entry, EntityState effectiveState, bool ownedChanged)
    {
        if (!_entityChangeEventOptions.Enable)
        {
            return;
        }

        var table = entry.Metadata.GetTableName();

        if (!IsChangeTrackedTable(table))
        {
            return;
        }

        var operation = effectiveState switch
        {
            EntityState.Added => EntityChangeOperations.Created,
            EntityState.Deleted => EntityChangeOperations.Deleted,
            EntityState.Modified => EntityChangeOperations.Updated,
            _ => ownedChanged ? EntityChangeOperations.Updated : null
        };

        if (operation is null)
        {
            return;
        }

        _entityChangeBuffer.Add((table!, operation, entry.Entity.Id));
    }

    private EntityChangesCommittedEvent? BuildEntityChangesEvent(IReadOnlyCollection<(string Table, string Operation, Guid Id)> changes)
    {
        if (changes.Count == 0)
        {
            return null;
        }

        var entityChangesEvent = CreateEntityChangesEvent();

        AppendEntityChanges(entityChangesEvent, changes);

        return entityChangesEvent;
    }

    private EntityChangesCommittedEvent CreateEntityChangesEvent()
    {
        var correlationContext = _correlationContextProvider.Get() ?? CorrelationContext.Empty;

        return new EntityChangesCommittedEvent
        {
            CorrelationId = correlationContext.CorrelationId,
            Domain = _biAppOptions?.Domain ?? string.Empty,
            TenantId = correlationContext.TenantId
        };
    }

    private static void AppendEntityChanges(EntityChangesCommittedEvent entityChangesEvent, IEnumerable<(string Table, string Operation, Guid Id)> changes)
    {
        foreach (var change in changes)
        {
            var item = entityChangesEvent.Changes.FirstOrDefault(c =>
                string.Equals(c.Table, change.Table, StringComparison.OrdinalIgnoreCase) &&
                c.Operation == change.Operation);

            if (item is null)
            {
                item = new EntityChangeItem { Table = change.Table, Operation = change.Operation };

                entityChangesEvent.Changes.Add(item);
            }

            if (!item.Ids.Contains(change.Id))
            {
                item.Ids.Add(change.Id);
            }
        }
    }

    private void EnsureNotSavingOnHttpGet(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var http = _httpContextAccessor?.HttpContext;

        if (http is null)
        {
            return;
        }

        if (http.WebSockets.IsWebSocketRequest)
        {
            return;
        }

        if (!HttpMethods.IsGet(http.Request.Method))
        {
            return;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            throw new InvalidOperationException("Persisting changes is not allowed during an HTTP GET request.");
        }
    }

    private void UpdateEntities(DbContext? dbContext, List<EntityEntry<IBaseEntity>>? precomputedEntries = null)
    {
        if (dbContext is not BaseDbContext baseDbContext)
        {
            return;
        }

        var correlationContext = _correlationContextProvider.Get() ?? CorrelationContext.Empty;

        var effectiveEntries = precomputedEntries ?? baseDbContext.ChangeTracker.Entries<IBaseEntity>().ToList();

        foreach (var entry in effectiveEntries)
        {
            // Compute once; reused in both timestamp logic and CollectEntityEvents below.
            var ownedChanged = entry.HasChangedOwnedEntities();
            var isSoftDelete = false;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CorrelationId = correlationContext.CorrelationId;

                if (correlationContext.CorrelationId == Guid.Empty)
                {
                    _logger.LogWarning(
                        "Entity {EntityType} added to {DbContextType} with an empty CorrelationId — CorrelationContext was not available at save time.",
                        entry.Entity.GetType().Name,
                        baseDbContext.GetType().Name);
                }

                if (entry.Entity is ITenantBaseEntity tenantEntity && tenantEntity.TenantId == Guid.Empty && correlationContext.TenantId.HasValue)
                {
                    tenantEntity.TenantId = correlationContext.TenantId.Value;
                }

                entry.Entity.CreatedBy = correlationContext.User?.Id ?? correlationContext.ClientId;
                entry.Entity.Created = _dateTimeService.Today;
                entry.Entity.CreatedTime = _dateTimeService.TimeNow;
            }
            else if (entry.State == EntityState.Modified || ownedChanged)
            {
                entry.Entity.CorrelationId = correlationContext.CorrelationId;

                if (entry.Entity is ITenantBaseEntity tenantEntity && tenantEntity.TenantId == Guid.Empty && correlationContext.TenantId.HasValue)
                {
                    tenantEntity.TenantId = correlationContext.TenantId.Value;
                }

                entry.Entity.UpdatedBy = correlationContext.User?.Id ?? correlationContext.ClientId;
                entry.Entity.Updated = _dateTimeService.Today;
                entry.Entity.UpdatedTime = _dateTimeService.TimeNow;
            }
            else if (entry.State == EntityState.Deleted && !baseDbContext.HardDeleteEnabled)
            {
                entry.State = EntityState.Modified;
                entry.Entity.Deleted = true;
                isSoftDelete = true;

                entry.Entity.CorrelationId = correlationContext.CorrelationId;

                if (entry.Entity is ITenantBaseEntity tenantEntity && tenantEntity.TenantId == Guid.Empty && correlationContext.TenantId.HasValue)
                {
                    tenantEntity.TenantId = correlationContext.TenantId.Value;
                }

                entry.Entity.UpdatedBy = correlationContext.User?.Id ?? correlationContext.ClientId;
                entry.Entity.Updated = _dateTimeService.Today;
                entry.Entity.UpdatedTime = _dateTimeService.TimeNow;
            }

            CollectEntityEvents(entry, isSoftDelete ? EntityState.Deleted : entry.State, ownedChanged);
            CollectEntityChange(entry, isSoftDelete ? EntityState.Deleted : entry.State, ownedChanged);
        }
    }

    private void CollectEntityEvents(EntityEntry<IBaseEntity> entry, EntityState effectiveState, bool ownedChanged)
    {
        IBaseEvent? baseEvent = null;

        if (effectiveState == EntityState.Added)
        {
            baseEvent = entry.Entity.AddCreatedEvent(_mapper, null);
        }
        else if (effectiveState == EntityState.Deleted)
        {
            baseEvent = entry.Entity.AddDeletedEvent(_mapper, null);
        }
        else if (effectiveState == EntityState.Modified || ownedChanged)
        {
            baseEvent = entry.Entity.AddUpdatedEvent(_mapper, null);
        }

        if (baseEvent is BaseEvent concreteEvent)
        {
            _entityEventBuffer.Add(concreteEvent);
        }
    }

    private void CollectAuditEntries(DbContext? dbContext, List<EntityEntry<IBaseEntity>>? precomputedEntries = null)
    {
        if (dbContext is not BaseDbContext baseDbContext)
        {
            return;
        }

        var correlationContext = _correlationContextProvider.Get() ?? CorrelationContext.Empty;
        var userId = correlationContext.User?.Id;
        var clientId = correlationContext.ClientId;

        var effectiveEntries = precomputedEntries ?? baseDbContext.ChangeTracker.Entries<IBaseEntity>().ToList();

        foreach (var entry in effectiveEntries)
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
                        var modifiedProps = entry.Properties
                            .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey())
                            .ToList();

                        if (modifiedProps.Count == 0)
                        {
                            continue;
                        }

                        var before = new Dictionary<string, object?>();
                        var changedFields = new List<string>();

                        foreach (var prop in modifiedProps)
                        {
                            var original = prop.OriginalValue;
                            var current = prop.CurrentValue;

                            before[prop.Metadata.Name] = original;

                            if (HasValueChanged(original, current))
                            {
                                changedFields.Add(prop.Metadata.Name);
                            }
                        }

                        if (changedFields.Count == 0)
                        {
                            continue;
                        }

                        beforeJson = JsonSerializer.Serialize(before);
                        afterJson = JsonSerializer.Serialize(entry.Entity, entry.Entity.GetType());
                        changedFieldsJson = JsonSerializer.Serialize(changedFields);
                        changeCount = changedFields.Count;

                        break;
                    }

                case EntityState.Deleted:
                    beforeJson = JsonSerializer.Serialize(entry.Entity, entry.Entity.GetType());
                    changeCount = 1;

                    break;
            }

            if (changeCount == 0)
            {
                continue;
            }

            _auditBuffer.Add(new AuditLogEvent
            {
                CorrelationId = correlationContext.CorrelationId,
                ApplicationId = correlationContext.ApplicationId,
                TenantId = correlationContext.TenantId,
                UserId = userId,
                ApplicationClientId = clientId,
                ServiceName = _biAppOptions?.Domain ?? string.Empty,
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id.ToString(),
                BeforeJson = beforeJson,
                AfterJson = afterJson,
                ChangedFieldsJson = changedFieldsJson,
                ChangeCount = changeCount,
                Created = DateOnly.FromDateTime(_dateTimeService.Now.ToUniversalTime()),
                CreatedTime = TimeOnly.FromDateTime(_dateTimeService.Now.ToUniversalTime()),
                CreatedBy = userId ?? clientId ?? Guid.Empty
            });
        }
    }

    private static bool HasValueChanged(object? original, object? current)
    {
        if (original is null && current is null)
        {
            return false;
        }

        if (original is null || current is null)
        {
            return true;
        }

        if (original is Guid guidOrig && current is Guid guidCur)
        {
            return guidOrig != guidCur;
        }

        if (original is DateTime dtOrig && current is DateTime dtCur)
        {
            return dtOrig != dtCur;
        }

        if (original is DateOnly dOrig && current is DateOnly dCur)
        {
            return dOrig != dCur;
        }

        if (original is TimeOnly tOrig && current is TimeOnly tCur)
        {
            return tOrig != tCur;
        }

        return !original.Equals(current);
    }

    private async Task PublishEntityEventsAsync()
    {
        if (_rabbitMQClient is null || _entityEventBuffer.Count == 0)
        {
            return;
        }

        var snapshot = _entityEventBuffer.ToList();
        _entityEventBuffer.Clear();

        foreach (var evt in snapshot)
        {
            await _rabbitMQClient.PublishAsync(evt);
        }
    }

    private async Task PublishAuditLogEventsAsync()
    {
        if (_rabbitMQClient is null || _auditBuffer.Count == 0)
        {
            return;
        }

        var snapshot = _auditBuffer.ToList();
        _auditBuffer.Clear();

        foreach (var auditEvent in snapshot)
        {
            await _rabbitMQClient.PublishAsync(auditEvent);
        }
    }

    private async Task SafePublishAsync(Task publishTask)
    {
        try
        {
            await publishTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background RabbitMQ publish failed in EntitySaveChangesInterceptor");
        }
    }

    private static bool IsAuditable(object entity)
    {
        var type = entity.GetType();
        var nowTicks = DateTime.UtcNow.Ticks;

        if (_auditableCache.TryGetValue(type, out var cached) && nowTicks < cached.ExpiresAtTicks)
        {
            return cached.Value;
        }

        var attr = type.GetCustomAttributes(typeof(AuditableAttribute), false).FirstOrDefault() as AuditableAttribute;
        var result = attr?.Enabled != false;

        _auditableCache[type] = (result, nowTicks + _auditableCacheTtlTicks);

        return result;
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