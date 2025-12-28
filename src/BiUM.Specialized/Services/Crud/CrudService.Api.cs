using BiUM.Specialized.Common.API;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.Crud;

public partial class CrudService
{
    public async Task<ApiEmptyResponse> SaveAsync(string code, Dictionary<string, object?> data, CancellationToken cancellationToken)
    {
        var version = await GetVersionByCodeAsync(code, cancellationToken);

        if (TryGetGuid(data, "Id", out var id))
        {
            await UpdateInternalAsync(version, id, data, cancellationToken);
        }
        else
        {
            await CreateInternalAsync(version, data, cancellationToken);
        }

        return new ApiEmptyResponse();
    }

    public async Task<ApiEmptyResponse> DeleteAsync(string code, Guid id, bool hardDelete, CancellationToken cancellationToken)
    {
        var version = await GetVersionByCodeAsync(code, cancellationToken);
        var (api2db, _) = BuildMaps(version);

        var dbType = _configuration.GetValue<string>("DatabaseType") ?? "PostgreSQL";
        var schema = ResolveSchema(version.TenantId);
        string QI(string s) => dbType == "PostgreSQL" ? QuotePg(s) : QuoteMs(s);
        var table = dbType == "PostgreSQL" ? $"{QuotePg(schema)}.{QuotePg(version.TableName)}" : $"[{schema}].[{version.TableName}]";

        if (hardDelete)
        {
            var sqlHard = $"DELETE FROM {table} WHERE {QI(api2db["id"])} = @p0";

            _ = await ExecuteSqlAsync(sqlHard, new object?[] { id }, cancellationToken);

            return new ApiEmptyResponse();
        }
        else
        {
            var set = $"{QI(api2db["deleted"])} = {(dbType == "PostgreSQL" ? "true" : "1")}, {QI(api2db["updatedBy"])} = '{_correlationContext.User?.Id}', {QI(api2db["updated"])} = {NowDateSql(dbType)}, {QI(api2db["updatedTime"])} = {NowTimeSql(dbType)}";
            var sqlSoft = $"UPDATE {table} SET {set} WHERE {QI(api2db["id"])} = @p0 AND {QI(api2db["deleted"])} = {(dbType == "PostgreSQL" ? "false" : "0")}";

            _ = await ExecuteSqlAsync(sqlSoft, new object?[] { id }, cancellationToken);

            return new ApiEmptyResponse();
        }
    }

    public async Task<IDictionary<string, object?>> GetAsync(string code, Guid id, CancellationToken cancellationToken)
    {
        var version = await GetVersionByCodeAsync(code, cancellationToken);
        var (api2db, db2api) = BuildMaps(version);

        var dbType = _configuration.GetValue<string>("DatabaseType") ?? "PostgreSQL";
        var schema = ResolveSchema(version.TenantId);
        string QI(string s) => dbType == "PostgreSQL" ? QuotePg(s) : QuoteMs(s);
        var table = dbType == "PostgreSQL" ? $"{QuotePg(schema)}.{QuotePg(version.TableName)}" : $"[{schema}].[{version.TableName}]";

        var selectCols = db2api.Select(kv => $"{QI(kv.Key)} AS {QI(kv.Value)}").ToList();
        var select = string.Join(",", selectCols);

        var whereId = $"{QI(api2db["id"])} = @p0 AND {QI(api2db["deleted"])} = {(dbType == "PostgreSQL" ? "false" : "0")}";
        var sql = dbType == "PostgreSQL" ? $"SELECT {select} FROM {table} WHERE {whereId} LIMIT 1" : $"SELECT TOP 1 {select} FROM {table} WHERE {whereId}";

        var row = await QuerySingleRowAsync(sql, [id], cancellationToken);

        return row ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<PaginatedApiResponse<IDictionary<string, object?>>> GetListAsync(string code, Dictionary<string, string> query, CancellationToken ct)
    {
        var version = await GetVersionByCodeAsync(code, ct);
        var (api2db, db2api) = BuildMaps(version);

        var dbType = _configuration.GetValue<string>("DatabaseType") ?? "PostgreSQL";
        var schema = ResolveSchema(version.TenantId);
        string QI(string s) => dbType == "PostgreSQL" ? QuotePg(s) : QuoteMs(s);
        var table = dbType == "PostgreSQL" ? $"{QuotePg(schema)}.{QuotePg(version.TableName)}" : $"[{schema}].[{version.TableName}]";

        var selectCols = db2api.Select(kv => $"{QI(kv.Key)} AS {QI(kv.Value)}").ToList();
        var select = string.Join(",", selectCols);

        var where = new StringBuilder($"WHERE {QI(api2db["deleted"])} = {(dbType == "PostgreSQL" ? "false" : "0")}");
        var parms = new List<object?>();
        var p = 0;

        var allowedApi = new HashSet<string>(version.DomainCrudVersionColumns!.Select(x => x.PropertyName).Concat(BaseApiProperties), StringComparer.OrdinalIgnoreCase);
        var dynDict = version.DomainCrudVersionColumns!.ToDictionary(c => c.PropertyName, StringComparer.OrdinalIgnoreCase);

        foreach (var kv in query)
        {
            if (string.Equals(kv.Key, "PageStart", StringComparison.OrdinalIgnoreCase)) { continue; }
            if (string.Equals(kv.Key, "PageSize", StringComparison.OrdinalIgnoreCase)) { continue; }
            if (string.Equals(kv.Key, "Sort", StringComparison.OrdinalIgnoreCase)) { continue; }

            if (!allowedApi.Contains(kv.Key)) { continue; }

            object? norm;

            if (BaseApiProperties.Contains(kv.Key))
            {
                if (!TryNormalizeBaseFilterValue(kv.Key, kv.Value, out norm)) { continue; }
            }
            else
            {
                var meta = dynDict[kv.Key];
                norm = NormalizeValue(meta.DataTypeId, kv.Value, dbType);
            }

            var dbCol = api2db[kv.Key];

            if (norm is string stringNorm)
            {
                where.Append($" AND LOWER({QI(dbCol)}) LIKE LOWER(@p{p})");
                parms.Add($"%{stringNorm}%");
            }
            else
            {
                where.Append($" AND {QI(dbCol)} = @p{p}");
                parms.Add(norm);
            }

            p++;
        }

        var order = "";
        if (query.TryGetValue("Sort", out var sortSpec) && !string.IsNullOrWhiteSpace(sortSpec))
        {
            var pieces = new List<string>();
            foreach (var part in sortSpec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var col = part;
                var asc = true;

                if (col.StartsWith("-", StringComparison.Ordinal)) { asc = false; col = col.Substring(1); }
                else if (col.Contains(':')) { var seg = col.Split(':', 2); col = seg[0]; asc = !seg[1].Equals("desc", StringComparison.OrdinalIgnoreCase); }
                else if (col.EndsWith(" desc", StringComparison.OrdinalIgnoreCase)) { asc = false; col = col[..^5].TrimEnd(); }
                else if (col.EndsWith(" asc", StringComparison.OrdinalIgnoreCase)) { asc = true; col = col[..^4].TrimEnd(); }

                if (!allowedApi.Contains(col)) { continue; }

                var dbCol = api2db[col];

                pieces.Add($"{QI(dbCol)} {(asc ? "ASC" : "DESC")}");
            }

            if (pieces.Count > 0) { order = " ORDER BY " + string.Join(",", pieces); }
        }
        else
        {
            order = $" ORDER BY {QI(api2db["created"])} DESC, {QI(api2db["createdTime"])} DESC";
        }

        var pageSize = query.TryGetValue("PageSize", out var psStr) && int.TryParse(psStr, out var ps) && ps > 0 ? ps : 20;
        var pageStart = query.TryGetValue("PageStart", out var pstStr) && int.TryParse(pstStr, out var pst) && pst >= 0 ? pst : 0;
        var pageNumber = (pageStart / pageSize) + 1;

        var limit = dbType == "PostgreSQL" ? $" LIMIT {pageSize} OFFSET {pageStart}" : $" OFFSET {pageStart} ROWS FETCH NEXT {pageSize} ROWS ONLY";

        var sqlCount = $"SELECT COUNT(1) FROM {table} {where}";
        var total = await QueryScalarLongAsync(sqlCount, parms.ToArray(), ct);

        var sql = $"SELECT {select} FROM {table} {where}{order}{limit}";
        var items = await QueryRowsAsync(sql, parms.ToArray(), ct);

        var payload = new List<IReadOnlyList<IDictionary<string, object?>>>() { items };

        return new PaginatedApiResponse<IDictionary<string, object?>>(items, (int)total, pageNumber, pageSize);
    }

    private static bool TryNormalizeBaseFilterValue(string key, string raw, out object? value)
    {
        value = null;

        if (key.Equals("active", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("deleted", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("test", StringComparison.OrdinalIgnoreCase))
        {
            if (bool.TryParse(raw, out var b)) { value = b; return true; }
            if (raw == "0") { value = false; return true; }
            if (raw == "1") { value = true; return true; }

            return false;
        }

        if (key.Equals("id", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("correlationId", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("tenantId", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("createdBy", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("updatedBy", StringComparison.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(raw, out var g)) { value = g; return true; }

            return false;
        }

        if (key.Equals("created", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("updated", StringComparison.OrdinalIgnoreCase))
        {
            if (DateOnly.TryParse(raw, out var d)) { value = d; return true; }
            if (DateTime.TryParse(raw, out var dt)) { value = DateOnly.FromDateTime(dt); return true; }

            return false;
        }

        if (key.Equals("createdTime", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("updatedTime", StringComparison.OrdinalIgnoreCase))
        {
            if (TimeOnly.TryParse(raw, out var t)) { value = t; return true; }
            if (DateTime.TryParse(raw, out var dt)) { value = TimeOnly.FromDateTime(dt); return true; }
            return false;
        }

        return false;
    }
}
