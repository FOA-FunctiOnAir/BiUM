using BiUM.Core.Authorization;
using BiUM.Core.Compensation;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.Compensation;

public sealed class CompensationService : ICompensationService
{
    private const string DbTypePostgresql = "PostgreSQL";

    private readonly BaseDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public CompensationService(IDbContext dbContext, IConfiguration configuration, ICorrelationContextAccessor correlationContextAccessor)
    {
        _dbContext = dbContext as BaseDbContext
            ?? throw new ArgumentException("IDbContext must be BaseDbContext for compensation.", nameof(dbContext));
        _configuration = configuration;
        _correlationContextAccessor = correlationContextAccessor;
    }

    // CommitSessionAsync/RollbackSessionAsync'in kendi SaveChangesAsync çağrısı, EntitySaveChangesInterceptor'ı
    // tekrar tetikler; CorrelationContext.CompensationSessionId hala doluyken bu, az önce Committed/RolledBack
    // olarak işaretlediğimiz entity'leri yeni bir Pending snapshot olarak tekrar track eder. Save sırasında
    // session id'yi geçici olarak temizleyip Apply()'ı no-op finalize yoluna düşürüyoruz.
    private async Task SaveChangesWithoutReprocessingAsync(CancellationToken cancellationToken)
    {
        var originalContext = _correlationContextAccessor.CorrelationContext;

        if (originalContext is not null)
        {
            _correlationContextAccessor.CorrelationContext = originalContext.WithCompensationSessionId(Guid.Empty);
        }

        try
        {
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (originalContext is not null)
            {
                _correlationContextAccessor.CorrelationContext = originalContext;
            }
        }
    }

    public async Task CommitSessionAsync(Guid compensationSessionId, CancellationToken cancellationToken)
    {
        var pending = await _dbContext.DomainCompensationSnapshots
            .Where(s => s.CompensationSessionId == compensationSessionId && s.State == (int)CompensationSnapshotRowState.Pending)
            .OrderBy(s => s.Version)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var snap in pending)
        {
            snap.State = (int)CompensationSnapshotRowState.Committed;
            snap.ProcessedAt = now;

            if (!string.IsNullOrEmpty(snap.EntityClrTypeName))
            {
                var clr = ResolveEntityType(snap.EntityClrTypeName);

                if (clr is null)
                {
                    continue;
                }

                var entity = await _dbContext.FindAsync(clr, [snap.EntityId], cancellationToken);

                if (entity is ICompensatableEntity c)
                {
                    c.CStatus = CompensationStatusCodes.Committed;
                    c.CompensationSessionId = null;
                }
            }
            else if (snap.ApplicationId.HasValue && !string.IsNullOrEmpty(snap.SnapshotTableName))
            {
                await CommitCrudRowAsync(snap, cancellationToken);
            }
        }

        await SaveChangesWithoutReprocessingAsync(cancellationToken);
    }

    public async Task RollbackSessionAsync(Guid compensationSessionId, CancellationToken cancellationToken)
    {
        var pending = await _dbContext.DomainCompensationSnapshots
            .Where(s => s.CompensationSessionId == compensationSessionId && s.State == (int)CompensationSnapshotRowState.Pending)
            .OrderByDescending(s => s.Version)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var snap in pending)
        {
            if (!string.IsNullOrEmpty(snap.EntityClrTypeName))
            {
                await RollbackEntitySnapshotAsync(snap, cancellationToken);
            }
            else if (snap.ApplicationId.HasValue && !string.IsNullOrEmpty(snap.SnapshotTableName))
            {
                await RollbackCrudSnapshotAsync(snap, cancellationToken);
            }

            snap.State = (int)CompensationSnapshotRowState.RolledBack;
            snap.ProcessedAt = now;
        }

        await SaveChangesWithoutReprocessingAsync(cancellationToken);
    }

    private async Task CommitCrudRowAsync(DomainCompensationSnapshot snap, CancellationToken cancellationToken)
    {
        var dbType = _configuration.GetValue<string>("DatabaseType") ?? DbTypePostgresql;
        var schema = ResolveSchema(snap.ApplicationId!.Value, snap.TenantId);
        var tableName = snap.SnapshotTableName ?? string.Empty;
        var table = dbType == DbTypePostgresql
            ? $"{QuotePg(schema)}.{QuotePg(tableName)}"
            : $"[{schema}].[{tableName}]";

        var idCol = dbType == DbTypePostgresql ? QuotePg("ID") : QuoteMs("ID");
        var cStatusCol = dbType == DbTypePostgresql ? QuotePg("C_STATUS") : QuoteMs("C_STATUS");
        var sessionCol = dbType == DbTypePostgresql ? QuotePg("COMPENSATION_SESSION_ID") : QuoteMs("COMPENSATION_SESSION_ID");

        var sql = $"UPDATE {table} SET {cStatusCol} = {{1}}, {sessionCol} = NULL WHERE {idCol} = {{0}}";

        _ = await _dbContext.Database.ExecuteSqlRawAsync(sql, [snap.EntityId, CompensationStatusCodes.Committed], cancellationToken);
    }

    private async Task RollbackCrudSnapshotAsync(DomainCompensationSnapshot snap, CancellationToken cancellationToken)
    {
        var dbType = _configuration.GetValue<string>("DatabaseType") ?? DbTypePostgresql;
        var schema = ResolveSchema(snap.ApplicationId!.Value, snap.TenantId);
        var tableName = snap.SnapshotTableName ?? string.Empty;
        var table = dbType == DbTypePostgresql
            ? $"{QuotePg(schema)}.{QuotePg(tableName)}"
            : $"[{schema}].[{tableName}]";

        var idCol = dbType == DbTypePostgresql ? QuotePg("ID") : QuoteMs("ID");

        switch ((CompensationSnapshotOperationType)snap.OperationType)
        {
            case CompensationSnapshotOperationType.Insert:
                {
                    var sql = $"DELETE FROM {table} WHERE {idCol} = {{0}}";
                    _ = await _dbContext.Database.ExecuteSqlRawAsync(sql, [snap.EntityId], cancellationToken);

                    break;
                }

            case CompensationSnapshotOperationType.Update:
                {
                    if (string.IsNullOrEmpty(snap.OldDataJson))
                    {
                        break;
                    }

                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(snap.OldDataJson);

                    if (dict is null || dict.Count == 0)
                    {
                        break;
                    }

                    var setParts = new List<string>();
                    var prms = new List<object> { snap.EntityId };
                    var p = 1;

                    foreach (var kv in dict)
                    {
                        if (string.Equals(kv.Key, "Id", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var col = kv.Key;
                        var dbCol = dbType == DbTypePostgresql ? QuotePg(col) : QuoteMs(col);
                        setParts.Add($"{dbCol} = {{{p}}}");
                        prms.Add(JsonElementToObject(kv.Value) ?? (object)DBNull.Value);
                        p++;
                    }

                    var cStatusCol = dbType == DbTypePostgresql ? QuotePg("C_STATUS") : QuoteMs("C_STATUS");
                    setParts.Add($"{cStatusCol} = {{{p}}}");
                    prms.Add(CompensationStatusCodes.Committed);

                    var setSql = string.Join(",", setParts);
                    var sqlU = $"UPDATE {table} SET {setSql} WHERE {idCol} = {{0}}";

                    _ = await _dbContext.Database.ExecuteSqlRawAsync(sqlU, prms, cancellationToken);

                    break;
                }

            case CompensationSnapshotOperationType.Delete:
                {
                    if (string.IsNullOrEmpty(snap.OldDataJson))
                    {
                        break;
                    }

                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(snap.OldDataJson);

                    if (dict is null || dict.Count == 0)
                    {
                        break;
                    }

                    var cols = new List<string>();
                    var vals = new List<string>();
                    var prms = new List<object>();
                    var p = 0;

                    foreach (var kv in dict.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        var dbCol = dbType == DbTypePostgresql ? QuotePg(kv.Key) : QuoteMs(kv.Key);
                        cols.Add(dbCol);
                        vals.Add($"{{{p}}}");
                        prms.Add(JsonElementToObject(kv.Value) ?? (object)DBNull.Value);
                        p++;
                    }

                    var sqlI = $"INSERT INTO {table} ({string.Join(",", cols)}) VALUES ({string.Join(",", vals)})";

                    _ = await _dbContext.Database.ExecuteSqlRawAsync(sqlI, prms, cancellationToken);

                    break;
                }
        }
    }

    private static object? JsonElementToObject(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt32(out var i) ? i : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => el.GetRawText()
        };
    }

    private async Task RollbackEntitySnapshotAsync(DomainCompensationSnapshot snap, CancellationToken cancellationToken)
    {
        var clr = ResolveEntityType(snap.EntityClrTypeName!);

        if (clr is null)
        {
            return;
        }

        switch ((CompensationSnapshotOperationType)snap.OperationType)
        {
            case CompensationSnapshotOperationType.Insert:
                {
                    var e = await _dbContext.FindAsync(clr, new object[] { snap.EntityId }, cancellationToken);

                    if (e is not null)
                    {
                        _dbContext.Remove(e);
                    }

                    break;
                }

            case CompensationSnapshotOperationType.Delete:
                {
                    if (string.IsNullOrEmpty(snap.OldDataJson))
                    {
                        return;
                    }

                    var instance = Activator.CreateInstance(clr);

                    if (instance is null)
                    {
                        return;
                    }

                    ApplyJsonToEntity(instance, snap.OldDataJson);

                    if (instance is IBaseEntity b)
                    {
                        b.Id = snap.EntityId;
                    }

                    _ = _dbContext.Add(instance);

                    break;
                }

            case CompensationSnapshotOperationType.Update:
                {
                    if (string.IsNullOrEmpty(snap.OldDataJson))
                    {
                        return;
                    }

                    var entity = await _dbContext.FindAsync(clr, new object[] { snap.EntityId }, cancellationToken);

                    if (entity is null)
                    {
                        return;
                    }

                    ApplyJsonToEntity(entity, snap.OldDataJson);

                    if (entity is ICompensatableEntity c)
                    {
                        c.CStatus = CompensationStatusCodes.Committed;
                        c.CompensationSessionId = null;
                    }

                    break;
                }
        }
    }

    private static void ApplyJsonToEntity(object entity, string json)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (dict is null)
        {
            return;
        }

        var type = entity.GetType();

        foreach (var kv in dict)
        {
            var prop = type.GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop is null || !prop.CanWrite)
            {
                continue;
            }

            try
            {
                var v = JsonSerializer.Deserialize(kv.Value.GetRawText(), prop.PropertyType);

                prop.SetValue(entity, v);
            }
            catch
            {
            }
        }
    }

    // Type.GetType(assemblyQualifiedName) çağrıldığı assembly'nin (BiUM.Specialized) bağlamında çözümleniyor;
    // hedef entity tipinin assembly'si (örn. BiApp.Authentication.Domain) versiyon dizesi tam eşleşmediğinde
    // veya varsayılan yükleme bağlamında doğrudan bulunamadığında sessizce null dönebiliyor. Bu durumda,
    // process'te zaten yüklü olan assembly'leri basit tip adına göre tarayarak geriye dönüş yapıyoruz.
    private static Type? ResolveEntityType(string assemblyQualifiedName)
    {
        var direct = Type.GetType(assemblyQualifiedName);

        if (direct is not null)
        {
            return direct;
        }

        var typeName = assemblyQualifiedName.Split(',')[0].Trim();

        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(typeName, throwOnError: false))
            .FirstOrDefault(t => t is not null);
    }

    private static string ResolveSchema(Guid applicationId, Guid tenantId)
    {
        var applicationIdString = applicationId.ToString("N");
        var tenantIdString = tenantId.ToString("N");
        var shorty = $"{applicationIdString[..16]}_{tenantIdString[..16]}";

        return $"t_{shorty}";
    }

    private static string QuotePg(string ident) => $"\"{ident.Replace("\"", "\"\"")}\"";

    private static string QuoteMs(string ident) => $"[{ident.Replace("]", "]]")}]";
}