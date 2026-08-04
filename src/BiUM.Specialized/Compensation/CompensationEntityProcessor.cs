using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Utils;
using BiUM.Core.Compensation;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace BiUM.Specialized.Compensation;

public static class CompensationEntityProcessor
{
    private static readonly ConcurrentDictionary<Type, (Type FactoryType, MethodInfo CreateMethod)?> FactoryMetadataCache = new();

    public static void Apply(DbContext context, ICorrelationContextProvider correlationContextProvider, IServiceProvider serviceProvider, IDateTimeService dateTimeService)
    {
        var correlation = correlationContextProvider.Get() ?? CorrelationContext.Empty;
        var sessionId = correlation.CompensationSessionId;

        List<(EntityEntry<ICompensatableEntity> Entry, DomainCompensationSnapshot Snapshot)>? toCommitEarly = null;

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

            // Bu session için zaten işlenmiş (örn. az önce erken-commit sibling context'e taşınırken bu entry'ye
            // dokunulmuştu, sibling'in kendi SaveChanges'i interceptor'ı tekrar tetikledi) — tekrar işlenmez.
            if (entity.CStatus is not null && entity.CompensationSessionId == currentSession)
            {
                continue;
            }

            var entityId = entity is IBaseEntity b ? b.Id : Guid.Empty;

            if (entityId != Guid.Empty)
            {
                var conflict = context.Set<DomainCompensationSnapshot>()
                    .AsNoTracking()
                    .Any(s =>
                        s.EntityId == entityId &&
                        s.State == (int)CompensationSnapshotRowState.Pending &&
                        s.CompensationSessionId != currentSession);

                if (conflict)
                {
                    throw new InvalidOperationException("compensation_session_conflict");
                }
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    {
                        entity.CStatus = CompensationStatusCodes.Insert;
                        entity.CompensationSessionId = currentSession;

                        var snap = AddSnapshot(context, entity, correlation, currentSession, CompensationSnapshotOperationType.Insert, entry, dateTimeService);

                        (toCommitEarly ??= []).Add((entry, snap));

                        break;
                    }

                case EntityState.Modified:
                    {
                        var isSoftDelete = entity is IBaseEntity be && be.Deleted;

                        entity.CStatus = isSoftDelete
                            ? (entity is IReadableCompensation ? CompensationStatusCodes.DeleteReadable : CompensationStatusCodes.Delete)
                            : (entity is IReadableCompensation ? CompensationStatusCodes.UpdateReadable : CompensationStatusCodes.Update);

                        entity.CompensationSessionId = currentSession;

                        var snap = AddSnapshot(context, entity, correlation, currentSession, isSoftDelete ? CompensationSnapshotOperationType.Delete : CompensationSnapshotOperationType.Update, entry, dateTimeService);

                        (toCommitEarly ??= []).Add((entry, snap));

                        break;
                    }

                case EntityState.Deleted:
                    break;
            }
        }

        // Compensatable satırlar, aynı CompensationSessionId'yi taşıyan başka bir MS/istek tarafından
        // (global query filter üzerinden) okunabilmesi için ambient (request-scoped) transaction'ı beklemeden
        // bağımsız bir connection ile hemen commit edilir. Normal (compensatable olmayan) entity'ler bu
        // metoda hiç girmez; onlar mevcut ambient transaction akışında kalmaya devam eder.
        if (toCommitEarly is not { Count: > 0 })
        {
            return;
        }

        using var sibling = TryCreateSiblingContext(context, serviceProvider);

        if (sibling is null)
        {
            // Bu DbContext tipi için IDbContextFactory kayıtlı değil (örn. henüz güncellenmemiş bir servis/test
            // ortamı) — entity'ler ana context'te bırakılır, önceki (ambient transaction'a bağlı) davranış korunur.
            return;
        }

        foreach (var (entry, snap) in toCommitEarly)
        {
            entry.State = EntityState.Detached;
            context.Entry(snap).State = EntityState.Detached;

            sibling.Add(entry.Entity);
            sibling.Add(snap);
        }

        sibling.SaveChanges();
    }

    // context.GetType() başına bir kez hesaplanıp cache'lenir; sonraki her çağrı sadece dictionary lookup +
    // DI'ın kendi (compiled/cache'li) GetService yolu + tek bir parametresiz MethodInfo.Invoke — Activator.CreateInstance
    // ile constructor'ı her seferinde reflection'la çözmekten belirgin şekilde daha ucuz.
    private static DbContext? TryCreateSiblingContext(DbContext context, IServiceProvider serviceProvider)
    {
        var contextType = context.GetType();

        var metadata = FactoryMetadataCache.GetOrAdd(contextType, static t =>
        {
            var factoryType = typeof(IDbContextFactory<>).MakeGenericType(t);
            var createMethod = factoryType.GetMethod(nameof(IDbContextFactory<DbContext>.CreateDbContext));

            return createMethod is null ? null : (factoryType, createMethod);
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

        return (DbContext?)metadata.Value.CreateMethod.Invoke(factory, null);
    }

    private static DomainCompensationSnapshot AddSnapshot(
        DbContext context,
        ICompensatableEntity entity,
        CorrelationContext correlation,
        Guid sessionId,
        CompensationSnapshotOperationType operation,
        EntityEntry entry,
        IDateTimeService dateTimeService)
    {
        var version = NextVersion(context, entity, sessionId);
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

    private static int NextVersion(DbContext context, ICompensatableEntity entity, Guid sessionId)
    {
        var entityId = entity is IBaseEntity b ? b.Id : Guid.Empty;

        var localMax = context.ChangeTracker.Entries<DomainCompensationSnapshot>()
            .Where(e => e.Entity.EntityId == entityId && e.Entity.CompensationSessionId == sessionId)
            .Select(e => e.Entity.Version)
            .DefaultIfEmpty(0)
            .Max();

        var dbMax = context.Set<DomainCompensationSnapshot>()
            .AsNoTracking()
            .Where(s => s.EntityId == entityId && s.CompensationSessionId == sessionId)
            .Select(s => (int?)s.Version)
            .Max() ?? 0;

        return Math.Max(localMax, dbMax) + 1;
    }
}
