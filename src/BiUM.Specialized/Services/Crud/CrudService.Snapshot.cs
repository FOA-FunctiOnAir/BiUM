using BiUM.Core.Common.Utils;
using BiUM.Core.Compensation;
using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.Crud;

public partial class CrudService
{
    private async Task AppendCrudCompensationSnapshotIfNeededAsync(
        DomainCrudVersion version,
        CompensationSnapshotOperationType operation,
        Guid entityId,
        string? oldJson,
        string? newJson,
        CancellationToken ct)
    {
        if (version.DomainCrud?.Compensatible != true)
        {
            return;
        }

        var session = CorrelationContext.CompensationSessionId;

        if (session is null || session == Guid.Empty)
        {
            return;
        }

        var v = await NextCrudSnapshotVersionAsync(entityId, session.Value, ct);
        var snap = new DomainCompensationSnapshot
        {
            Id = GuidGenerator.New(),
            CorrelationId = CorrelationContext.CorrelationId,
            TenantId = version.TenantId,
            ApplicationId = version.ApplicationId,
            SnapshotTableName = version.TableName,
            EntityName = version.TableName,
            EntityClrTypeName = null,
            EntityId = entityId,
            OperationType = (int)operation,
            CompensationSessionId = session.Value,
            OldDataJson = oldJson,
            NewDataJson = newJson,
            Version = v,
            State = (int)CompensationSnapshotRowState.Pending,
            ExpireAt = null,
            ProcessedAt = null
        };

        _ = DbContext.DomainCompensationSnapshots.Add(snap);

        _ = await DbContext.SaveChangesAsync(ct);
    }

    private async Task<int> NextCrudSnapshotVersionAsync(Guid entityId, Guid sessionId, CancellationToken ct)
    {
        var max = await DbContext.DomainCompensationSnapshots
            .AsNoTracking()
            .Where(s => s.EntityId == entityId && s.CompensationSessionId == sessionId)
            .Select(s => s.Version)
            .DefaultIfEmpty(0)
            .MaxAsync(ct);

        return max + 1;
    }

    private async Task<string?> GetCrudRowJsonUnfilteredAsync(DomainCrudVersion version, Guid id, CancellationToken ct)
    {
        var versionForMaps = await DbContext.DomainCrudVersions
            .Include(s => s.DomainCrudVersionColumns)
            .Include(s => s.DomainCrud)
            .FirstOrDefaultAsync(s => s.Id == version.Id, ct);

        if (versionForMaps is null)
        {
            return null;
        }

        var compensatible = versionForMaps.DomainCrud?.Compensatible == true;
        var (api2db, db2api) = BuildMaps(versionForMaps, compensatible);
        var dbType = _configuration.GetValue<string>("DatabaseType") ?? DbTypePostgresql;
        var schema = ResolveSchema(versionForMaps.ApplicationId, versionForMaps.TenantId);
        var table = dbType == DbTypePostgresql ? $"{QuotePg(schema)}.{QuotePg(versionForMaps.TableName)}" : $"[{schema}].[{versionForMaps.TableName}]";

        var selectCols = db2api.Select(kv => $"{Quote(kv.Key)} AS {Quote(kv.Value)}").ToList();
        var select = string.Join(",", selectCols);
        var whereId = $"{Quote(api2db["id"])} = @p0 AND {Quote(api2db["deleted"])} = {(dbType == DbTypePostgresql ? "false" : "0")}";
        var sql = dbType == DbTypePostgresql ? $"SELECT {select} FROM {table} WHERE {whereId} LIMIT 1" : $"SELECT TOP 1 {select} FROM {table} WHERE {whereId}";

        var row = await QuerySingleRowAsync(sql, [id], ct);

        return row is null ? null : JsonSerializer.Serialize(row);

        string Quote(string s) => dbType == DbTypePostgresql ? QuotePg(s) : QuoteMs(s);
    }
}