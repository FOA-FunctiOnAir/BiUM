using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Utils;
using BiUM.Core.Compensation;
using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;
using System.Text.Json;

namespace BiUM.Specialized.Compensation;

public static class CompensationEntityProcessor
{
    public static void Apply(DbContext context, ICorrelationContextProvider correlationContextProvider)
    {
        var correlation = correlationContextProvider.Get() ?? CorrelationContext.Empty;
        var sessionId = correlation.CompensationSessionId;

        foreach (var entry in context.ChangeTracker.Entries<ICompensatableEntity>())
        {
            if (entry.Entity is DomainCompensationSnapshot)
            {
                continue;
            }

            var entity = entry.Entity;

            if (sessionId is null || sessionId == Guid.Empty)
            {
                entity.CStatus = CompensationStatusCodes.Committed;

                continue;
            }

            var currentSession = sessionId.Value;
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
                    entity.CStatus = CompensationStatusCodes.Insert;

                    AddSnapshot(context, entity, correlation, currentSession, CompensationSnapshotOperationType.Insert, entry);

                    break;

                case EntityState.Modified:
                    {
                        var isSoftDelete = entity is IBaseEntity be && be.Deleted;

                        if (isSoftDelete)
                        {
                            entity.CStatus = entity is IReadableCompensation
                                ? CompensationStatusCodes.DeleteReadable
                                : CompensationStatusCodes.Delete;
                        }
                        else
                        {
                            entity.CStatus = entity is IReadableCompensation
                                ? CompensationStatusCodes.UpdateReadable
                                : CompensationStatusCodes.Update;
                        }

                        AddSnapshot(context, entity, correlation, currentSession, isSoftDelete ? CompensationSnapshotOperationType.Delete : CompensationSnapshotOperationType.Update, entry);

                        break;
                    }

                case EntityState.Deleted:
                    break;
            }
        }
    }

    private static void AddSnapshot(
        DbContext context,
        ICompensatableEntity entity,
        CorrelationContext correlation,
        Guid sessionId,
        CompensationSnapshotOperationType operation,
        EntityEntry entry)
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
            ProcessedAt = null
        };

        _ = context.Set<DomainCompensationSnapshot>().Add(snap);
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
            .Select(s => s.Version)
            .DefaultIfEmpty(0)
            .Max();

        return Math.Max(localMax, dbMax) + 1;
    }
}