using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Utils;
using BiUM.Core.Compensation;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Compensation;

public static class CompensationEntityProcessor
{
    private static readonly ConcurrentDictionary<Type, (Type FactoryType, Func<object, DbContext?> InvokeCreate)?> FactoryMetadataCache = new();

    public static void Apply(DbContext context, ICorrelationContextProvider correlationContextProvider, IServiceProvider serviceProvider, IDateTimeService dateTimeService)
    {
        var toCommitEarly = PrepareSnapshots(context, correlationContextProvider, dateTimeService);

        if (toCommitEarly is null)
        {
            return;
        }

        using var siblingScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        using var sibling = TryCreateSiblingContext(context, siblingScope.ServiceProvider);

        if (sibling is null)
        {
            return;
        }

        AttachToSibling(sibling, context, toCommitEarly);

        try
        {
            sibling.SaveChanges();

            var transaction = sibling.Database.CurrentTransaction;

            if (transaction is not null)
            {
                transaction.Commit();
                transaction.Dispose();
            }
        }
        catch
        {
            var transaction = sibling.Database.CurrentTransaction;

            if (transaction is not null)
            {
                transaction.Rollback();
                transaction.Dispose();
            }

            throw;
        }
    }

    public static async Task ApplyAsync(DbContext context, ICorrelationContextProvider correlationContextProvider, IServiceProvider serviceProvider, IDateTimeService dateTimeService, CancellationToken cancellationToken)
    {
        var toCommitEarly = PrepareSnapshots(context, correlationContextProvider, dateTimeService);

        if (toCommitEarly is null)
        {
            return;
        }

        using var siblingScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        using var sibling = TryCreateSiblingContext(context, siblingScope.ServiceProvider);

        if (sibling is null)
        {
            return;
        }

        AttachToSibling(sibling, context, toCommitEarly);

        try
        {
            await sibling.SaveChangesAsync(cancellationToken);

            var transaction = sibling.Database.CurrentTransaction;

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                await transaction.DisposeAsync();
            }
        }
        catch
        {
            var transaction = sibling.Database.CurrentTransaction;

            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                await transaction.DisposeAsync();
            }

            throw;
        }
    }

    private static List<(EntityEntry<ICompensatableEntity> Entry, DomainCompensationSnapshot Snapshot)>? PrepareSnapshots(
        DbContext context,
        ICorrelationContextProvider correlationContextProvider,
        IDateTimeService dateTimeService)
    {
        var correlation = correlationContextProvider.Get() ?? CorrelationContext.Empty;
        var sessionId = correlation.CompensationSessionId;

        var sessionCandidates = new List<EntityEntry<ICompensatableEntity>>();

        foreach (var entry in context.ChangeTracker.Entries<ICompensatableEntity>().ToList())
        {
            if (entry.Entity is DomainCompensationSnapshot)
            {
                continue;
            }

            var entity = entry.Entity;

            if (sessionId is null || sessionId == Guid.Empty)
            {
                entity.CStatus = CompensationStatusCodes.Committed;
                entity.CompensationSessionId = null;
                continue;
            }

            var currentSession = sessionId.Value;

            if (entity.CStatus is not null && entity.CompensationSessionId == currentSession)
            {
                continue;
            }

            sessionCandidates.Add(entry);
        }

        if (sessionCandidates.Count == 0 || sessionId is null || sessionId == Guid.Empty)
        {
            return null;
        }

        var currentSession2 = sessionId.Value;

        var allEntityIds = sessionCandidates
            .Where(e => e.Entity is IBaseEntity)
            .Select(e => ((IBaseEntity)e.Entity).Id)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (allEntityIds.Count > 0)
        {
            var hasConflict = context.Set<DomainCompensationSnapshot>()
                .AsNoTracking()
                .Any(s =>
                    allEntityIds.Contains(s.EntityId) &&
                    s.State == (int)CompensationSnapshotRowState.Pending &&
                    s.CompensationSessionId != currentSession2);

            if (hasConflict)
            {
                throw new InvalidOperationException("compensation_session_conflict");
            }
        }

        var snapshotCandidates = sessionCandidates
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();

        if (snapshotCandidates.Count == 0)
        {
            return null;
        }

        var snapshotEntityIds = snapshotCandidates
            .Where(e => e.Entity is IBaseEntity)
            .Select(e => ((IBaseEntity)e.Entity).Id)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        Dictionary<Guid, int> dbMaxVersions = snapshotEntityIds.Count > 0
            ? context.Set<DomainCompensationSnapshot>()
                .AsNoTracking()
                .Where(s => snapshotEntityIds.Contains(s.EntityId) && s.CompensationSessionId == currentSession2)
                .GroupBy(s => s.EntityId)
                .Select(g => new { EntityId = g.Key, MaxVersion = g.Max(s => s.Version) })
                .ToDictionary(x => x.EntityId, x => x.MaxVersion)
            : [];

        Dictionary<Guid, int> ctMaxVersions = snapshotEntityIds.Count > 0
            ? context.ChangeTracker.Entries<DomainCompensationSnapshot>()
                .Where(e => e.Entity.CompensationSessionId == currentSession2 &&
                            snapshotEntityIds.Contains(e.Entity.EntityId))
                .GroupBy(e => e.Entity.EntityId)
                .ToDictionary(g => g.Key, g => g.Max(e => e.Entity.Version))
            : [];

        var versionTracker = new Dictionary<Guid, int>();

        foreach (var id in snapshotEntityIds)
        {
            versionTracker[id] = Math.Max(
                dbMaxVersions.GetValueOrDefault(id, 0),
                ctMaxVersions.GetValueOrDefault(id, 0));
        }

        var toCommitEarly = new List<(EntityEntry<ICompensatableEntity> Entry, DomainCompensationSnapshot Snapshot)>();

        foreach (var entry in snapshotCandidates)
        {
            var entity = entry.Entity;
            var entityId = entity is IBaseEntity b ? b.Id : Guid.Empty;

            var baseVersion = versionTracker.GetValueOrDefault(entityId, 0);
            var nextVersion = baseVersion + 1;
            versionTracker[entityId] = nextVersion;

            var isSoftDelete = entry.State == EntityState.Modified && entity is IBaseEntity be && be.Deleted;

            switch (entry.State)
            {
                case EntityState.Added:
                    {
                        entity.CStatus = CompensationStatusCodes.Insert;
                        entity.CompensationSessionId = currentSession2;

                        var snap = BuildSnapshot(context, entity, correlation, currentSession2, CompensationSnapshotOperationType.Insert, entry, dateTimeService, nextVersion);

                        toCommitEarly.Add((entry, snap));

                        break;
                    }

                case EntityState.Modified:
                    {
                        entity.CStatus = isSoftDelete
                            ? (entity is IReadableCompensation ? CompensationStatusCodes.DeleteReadable : CompensationStatusCodes.Delete)
                            : (entity is IReadableCompensation ? CompensationStatusCodes.UpdateReadable : CompensationStatusCodes.Update);

                        entity.CompensationSessionId = currentSession2;

                        var snap = BuildSnapshot(context, entity, correlation, currentSession2, isSoftDelete ? CompensationSnapshotOperationType.Delete : CompensationSnapshotOperationType.Update, entry, dateTimeService, nextVersion);

                        toCommitEarly.Add((entry, snap));

                        break;
                    }
            }
        }

        return toCommitEarly.Count > 0 ? toCommitEarly : null;
    }

    private static void AttachToSibling(
        DbContext sibling,
        DbContext context,
        List<(EntityEntry<ICompensatableEntity> Entry, DomainCompensationSnapshot Snapshot)> toCommitEarly)
    {
        foreach (var (entry, snap) in toCommitEarly)
        {
            entry.State = EntityState.Detached;
            context.Entry(snap).State = EntityState.Detached;

            sibling.Entry(entry.Entity).State = EntityState.Added;
            sibling.Entry(snap).State = EntityState.Added;
        }
    }

    private static DbContext? TryCreateSiblingContext(DbContext context, IServiceProvider serviceProvider)
    {
        var contextType = context.GetType();

        var metadata = FactoryMetadataCache.GetOrAdd(contextType, static t =>
        {
            var factoryType = typeof(IDbContextFactory<>).MakeGenericType(t);
            var createMethod = factoryType.GetMethod(nameof(IDbContextFactory<DbContext>.CreateDbContext));

            if (createMethod is null)
            {
                return null;
            }

            var param = Expression.Parameter(typeof(object), "factory");
            var cast = Expression.Convert(param, factoryType);
            var call = Expression.Call(cast, createMethod);
            var convert = Expression.Convert(call, typeof(DbContext));
            var invoke = Expression.Lambda<Func<object, DbContext?>>(convert, param).Compile();

            return (factoryType, invoke);
        });

        if (metadata is null)
        {
            return null;
        }

        var factory = serviceProvider.GetService(metadata.Value.FactoryType);

        if (factory is null)
        {
            return null;
        }

        return metadata.Value.InvokeCreate(factory);
    }

    private static DomainCompensationSnapshot BuildSnapshot(
        DbContext context,
        ICompensatableEntity entity,
        CorrelationContext correlation,
        Guid sessionId,
        CompensationSnapshotOperationType operation,
        EntityEntry entry,
        IDateTimeService dateTimeService,
        int version)
    {
        var entityType = entity.GetType();
        var oldJson = operation == CompensationSnapshotOperationType.Insert
            ? null
            : JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));

        var snap = new DomainCompensationSnapshot
        {
            Id = GuidGenerator.New(),
            CorrelationId = correlation.CorrelationId,
            TenantId = entity is ITenantBaseEntity te ? te.TenantId : correlation.TenantId ?? Guid.Empty,
            ApplicationId = correlation.ApplicationId,
            EntityName = entityType.Name,
            EntityClrTypeName = entityType.AssemblyQualifiedName,
            EntityId = entity is IBaseEntity b ? b.Id : Guid.Empty,
            OperationType = (int)operation,
            CompensationSessionId = sessionId,
            OldDataJson = oldJson,
            NewDataJson = JsonSerializer.Serialize(entity, entityType),
            Version = version,
            State = (int)CompensationSnapshotRowState.Pending,
            ExpireAt = null,
            ProcessedAt = null,
            Created = dateTimeService.Today,
            CreatedTime = dateTimeService.TimeNow,
            CreatedBy = correlation.User?.Id ?? correlation.ClientId
        };

        _ = context.Set<DomainCompensationSnapshot>().Add(snap);

        return snap;
    }
}