using BiUM.Core.Common.Utils;
using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;
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
    private static string GenerateCreateTablePgSql(DomainCrud crud, IEnumerable<DomainCrudVersionColumn> cols)
    {
        var schema = ResolveSchema(crud.TenantId);

        var sb = new StringBuilder();
        var table = $"{Q(schema)}.{Q(crud.TableName)}";
        var tblName = crud.TableName;

        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {table} (");

        var fixedCols = new[]
        {
            $"    {Q("ID")} uuid NOT NULL DEFAULT gen_random_uuid()",
            $"    {Q("CORRELATION_ID")} uuid NOT NULL",
            $"    {Q("TENANT_ID")} uuid NOT NULL",
            $"    {Q("ACTIVE")} boolean NOT NULL DEFAULT true",
            $"    {Q("DELETED")} boolean NOT NULL DEFAULT false",
            $"    {Q("CREATED")} date NOT NULL DEFAULT (now()::date)",
            $"    {Q("CREATED_TIME")} time without time zone NOT NULL DEFAULT (now()::time)",
            $"    {Q("CREATED_BY")} uuid NULL",
            $"    {Q("UPDATED")} date NULL",
            $"    {Q("UPDATED_TIME")} time without time zone NULL",
            $"    {Q("UPDATED_BY")} uuid NULL",
            $"    {Q("TEST")} boolean NOT NULL DEFAULT false"
        };

        foreach (var line in fixedCols) sb.AppendLine(line + ",");

        var list = cols.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var c = list[i];
            var tail = i < list.Count - 1 ? "," : "";
            sb.AppendLine($"    {Q(c.ColumnName)} {ToPgSqlType(c)}{tail}");
        }

        sb.AppendLine($",   CONSTRAINT {Q($"PK_{tblName}")} PRIMARY KEY ({Q("ID")})");
        sb.AppendLine(");");

        return sb.ToString();
    }

    private static string GenerateDiffPgSql(DomainCrud crud, IEnumerable<DomainCrudVersionColumn> prevCols, IEnumerable<DomainCrudVersionColumn> newCols)
    {
        var schema = ResolveSchema(crud.TenantId);

        var sb = new StringBuilder();
        var table = $"{Q(schema)}.{Q(crud.TableName)}";

        sb.AppendLine(GenerateCreateTablePgSql(crud, newCols));

        var prevDict = prevCols.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
        var newDict = newCols.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);

        foreach (var add in newDict.Keys.Except(prevDict.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var c = newDict[add];

            sb.AppendLine($"ALTER TABLE {table} ADD COLUMN IF NOT EXISTS {Q(c.ColumnName)} {ToPgSqlType(c)};");
        }

        foreach (var drop in prevDict.Keys.Except(newDict.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var c = prevDict[drop];

            sb.AppendLine($"ALTER TABLE {table} DROP COLUMN IF EXISTS {Q(c.ColumnName)};");
        }

        foreach (var common in newDict.Keys.Intersect(prevDict.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var oldC = prevDict[common];
            var newC = newDict[common];

            if (!ColumnSignatureEquals(oldC, newC))
            {
                sb.AppendLine($"ALTER TABLE {table} ALTER COLUMN {Q(newC.ColumnName)} TYPE {ToPgSqlType(newC)};");
            }
        }

        return sb.ToString();
    }

    private static string ToPgSqlType(DomainCrudVersionColumn c)
    {
        var useTimeZoneInTimestamp = false;

        if (c.DataTypeId == Ids.DataType.String)
        {
            if (c.MaxLength.HasValue && c.MaxLength.Value > 0) return $"varchar({c.MaxLength.Value})";

            return "text";
        }
        if (c.DataTypeId == Ids.DataType.Guid) return "uuid";
        if (c.DataTypeId == Ids.DataType.Integer) return "integer";
        if (c.DataTypeId == Ids.DataType.Decimal) return "numeric(18,2)";
        if (c.DataTypeId == Ids.DataType.Boolean) return "boolean";
        if (c.DataTypeId == Ids.DataType.DateTime) return useTimeZoneInTimestamp ? "timestamp with time zone" : "timestamp without time zone";
        if (c.DataTypeId == Ids.DataType.DateOnly) return "date";
        if (c.DataTypeId == Ids.DataType.TimeOnly) return "time without time zone";
        if (c.DataTypeId == Ids.DataType.Object) return "jsonb";

        return "text";
    }

    private static string Q(string ident) => $"\"{ident.Replace("\"", "\"\"")}\"";

    private static string GenerateCreateTableMsSql(DomainCrud crud, IEnumerable<DomainCrudVersionColumn> cols)
    {
        var schema = ResolveSchema(crud.TenantId);

        var sb = new StringBuilder();
        var table = $"[{schema}].[{Safe(crud.TableName)}]";

        sb.AppendLine($"IF OBJECT_ID(N'{table}', N'U') IS NULL");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"    CREATE TABLE {table} (");

        var fixedCols = new[]
        {
            "        [ID] uniqueidentifier NOT NULL CONSTRAINT [DF_" + Safe(crud.TableName) + "_ID] DEFAULT NEWID()",
            "        [CORRELATION_ID] uniqueidentifier NOT NULL",
            "        [TENANT_ID] uniqueidentifier NOT NULL",
            "        [ACTIVE] bit NOT NULL CONSTRAINT [DF_" + Safe(crud.TableName) + "_ACTIVE] DEFAULT (1)",
            "        [DELETED] bit NOT NULL CONSTRAINT [DF_" + Safe(crud.TableName) + "_DELETED] DEFAULT (0)",
            "        [CREATED] date NOT NULL CONSTRAINT [DF_" + Safe(crud.TableName) + "_CREATED] DEFAULT (CAST(GETDATE() AS date))",
            "        [CREATED_TIME] time(7) NOT NULL CONSTRAINT [DF_" + Safe(crud.TableName) + "_CREATED_TIME] DEFAULT (CAST(GETDATE() AS time))",
            "        [CREATED_BY] uniqueidentifier NULL",
            "        [UPDATED] date NULL",
            "        [UPDATED_TIME] time(7) NULL",
            "        [UPDATED_BY] uniqueidentifier NULL",
            "        [TEST] bit NOT NULL CONSTRAINT [DF_" + Safe(crud.TableName) + "_TEST] DEFAULT (0)"
        };

        foreach (var line in fixedCols) sb.AppendLine(line + ",");

        var list = cols.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var c = list[i];
            var tail = i < list.Count - 1 ? "," : "";
            sb.AppendLine($"        [{Safe(c.ColumnName)}] {ToMsSqlType(c)} NULL{tail}");
        }

        sb.AppendLine($"        ,CONSTRAINT [PK_{Safe(crud.TableName)}] PRIMARY KEY CLUSTERED ([ID] ASC)");
        sb.AppendLine("    );");
        sb.AppendLine("END;");

        return sb.ToString();
    }

    private static string GenerateDiffMsSql(DomainCrud crud, IEnumerable<DomainCrudVersionColumn> prevCols, IEnumerable<DomainCrudVersionColumn> newCols)
    {
        var schema = ResolveSchema(crud.TenantId);

        var sb = new StringBuilder();
        var table = $"[{schema}].[{Safe(crud.TableName)}]";

        sb.AppendLine($"IF OBJECT_ID(N'{table}', N'U') IS NULL");
        sb.AppendLine("BEGIN");
        sb.Append(GenerateCreateTableMsSql(crud, newCols));
        sb.AppendLine("    RETURN;");
        sb.AppendLine("END;");

        var prevDict = prevCols.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
        var newDict = newCols.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);

        foreach (var add in newDict.Keys.Except(prevDict.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var c = newDict[add];

            sb.AppendLine($"IF COL_LENGTH(N'{table}', N'{Safe(c.ColumnName)}') IS NULL");
            sb.AppendLine($"    ALTER TABLE {table} ADD [{Safe(c.ColumnName)}] {ToMsSqlType(c)} NULL;");
        }

        foreach (var drop in prevDict.Keys.Except(newDict.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var c = prevDict[drop];

            sb.AppendLine($"IF COL_LENGTH(N'{table}', N'{Safe(c.ColumnName)}') IS NOT NULL");
            sb.AppendLine($"    ALTER TABLE {table} DROP COLUMN [{Safe(c.ColumnName)}];");
        }

        foreach (var common in newDict.Keys.Intersect(prevDict.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var oldC = prevDict[common];
            var newC = newDict[common];

            if (!ColumnSignatureEquals(oldC, newC))
            {
                sb.AppendLine($"IF COL_LENGTH(N'{table}', N'{Safe(newC.ColumnName)}') IS NOT NULL");
                sb.AppendLine($"    ALTER TABLE {table} ALTER COLUMN [{Safe(newC.ColumnName)}] {ToMsSqlType(newC)} NULL;");
            }
        }

        return sb.ToString();
    }

    private static string ToMsSqlType(DomainCrudVersionColumn c)
    {
        if (c.DataTypeId == Ids.DataType.String)
        {
            var len = c.MaxLength.HasValue && c.MaxLength > 0 ? c.MaxLength.Value.ToString() : "MAX";

            return $"nvarchar({len})";
        }
        if (c.DataTypeId == Ids.DataType.Guid) return "uniqueidentifier";
        if (c.DataTypeId == Ids.DataType.Integer) return "int";
        if (c.DataTypeId == Ids.DataType.Decimal) return "decimal(18,2)";
        if (c.DataTypeId == Ids.DataType.Boolean) return "bit";
        if (c.DataTypeId == Ids.DataType.DateTime) return "datetime2(7)";
        if (c.DataTypeId == Ids.DataType.DateOnly) return "date";
        if (c.DataTypeId == Ids.DataType.TimeOnly) return "time(7)";
        if (c.DataTypeId == Ids.DataType.Object) return "nvarchar(MAX)";

        return "nvarchar(MAX)";
    }

    public static readonly HashSet<string> BaseColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "ID","CORRELATION_ID","TENANT_ID","ACTIVE","DELETED",
        "CREATED","CREATED_TIME","CREATED_BY","UPDATED","UPDATED_TIME","UPDATED_BY","TEST"
    };

    public static readonly HashSet<string> BaseApiProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "id","correlationId","tenantId","active","deleted",
        "created","createdTime","createdBy","updated","updatedTime","updatedBy","test"
    };

    private static (Dictionary<string, string> api2db, Dictionary<string, string> db2api) BuildMaps(DomainCrudVersion version)
    {
        var api2db = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = "ID",
            ["correlationId"] = "CORRELATION_ID",
            ["tenantId"] = "TENANT_ID",
            ["active"] = "ACTIVE",
            ["deleted"] = "DELETED",
            ["created"] = "CREATED",
            ["createdTime"] = "CREATED_TIME",
            ["createdBy"] = "CREATED_BY",
            ["updated"] = "UPDATED",
            ["updatedTime"] = "UPDATED_TIME",
            ["updatedBy"] = "UPDATED_BY",
            ["test"] = "TEST"
        };
        var db2api = api2db.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var c in version.DomainCrudVersionColumns!)
        {
            api2db[c.PropertyName] = c.ColumnName;
            db2api[c.ColumnName] = c.PropertyName;
        }

        return (api2db, db2api);
    }

    public static string QuotePg(string ident) => $"\"{ident.Replace("\"", "\"\"")}\"";

    public static string QuoteMs(string ident) => $"[{ident.Replace("]", "]]")}]";

    public static string NowDateSql(string db) => db == "PostgreSQL" ? "now()::date" : "CAST(GETDATE() AS date)";

    public static string NowTimeSql(string db) => db == "PostgreSQL" ? "now()::time" : "CAST(GETDATE() AS time)";

    public static string GenUuidSqlDefault(string db) => db == "PostgreSQL" ? "gen_random_uuid()" : "NEWID()";

    private static string ResolveSchema(Guid tenantId)
    {
        var compact = tenantId.ToString("N");
        var shorty = compact[..16];

        return $"t_{shorty}";
    }

    private static string GenerateEnsureSchemaPgSql(string schema) => $"CREATE SCHEMA IF NOT EXISTS {Q(schema)};";

    private static string GenerateEnsurePgcryptoPgSql() => "CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";";

    private static string GenerateEnsureSchemaMsSql(string schema)
    {
        var s = Safe(schema);

        return
$@"IF SCHEMA_ID(N'{s}') IS NULL
BEGIN
    EXEC('CREATE SCHEMA [{s}] AUTHORIZATION [dbo]');
END;
";
    }

    private static bool ColumnSignatureEquals(DomainCrudVersionColumn a, DomainCrudVersionColumn b) => a.DataTypeId == b.DataTypeId && (a.MaxLength ?? -1) == (b.MaxLength ?? -1);
    private static string Safe(string identifier) => identifier.Replace("]", "]]");

    private static bool TryGetGuid(IDictionary<string, object?> dict, string key, out Guid guid)
    {
        guid = Guid.Empty;

        if (!dict.TryGetValue(key, out var v) || v is null)
        {
            return false;
        }

        if (v is Guid g)
        {
            guid = g;

            return true;
        }

        return Guid.TryParse(v.ToString(), out guid);
    }

    private static object? NormalizeValue(Guid dataTypeId, object? value, string db)
    {
        if (value is null) return null;

        if (dataTypeId == Ids.DataType.Guid)
        {
            if (value is Guid g) return g;
            if (Guid.TryParse(value.ToString(), out var gg)) return gg;

            throw new ArgumentException("Invalid Guid");
        }
        if (dataTypeId == Ids.DataType.Integer)
        {
            if (value is int i) return i;
            if (int.TryParse(value.ToString(), out var ii)) return ii;

            throw new ArgumentException("Invalid Integer");
        }
        if (dataTypeId == Ids.DataType.Decimal)
        {
            if (value is decimal d) return d;
            if (decimal.TryParse(value.ToString(), out var dd)) return dd;

            throw new ArgumentException("Invalid Decimal");
        }
        if (dataTypeId == Ids.DataType.Boolean)
        {
            if (value is bool b) return b;
            if (bool.TryParse(value.ToString(), out var bb)) return bb;

            throw new ArgumentException("Invalid Boolean");
        }
        if (dataTypeId == Ids.DataType.DateOnly)
        {
            if (value is DateOnly d) return d;
            if (DateOnly.TryParse(value.ToString(), out var dd)) return dd;
            if (DateTime.TryParse(value.ToString(), out var dt)) return DateOnly.FromDateTime(dt);

            throw new ArgumentException("Invalid DateOnly");
        }
        if (dataTypeId == Ids.DataType.TimeOnly)
        {
            if (value is TimeOnly t) return t;
            if (TimeOnly.TryParse(value.ToString(), out var tt)) return tt;
            if (DateTime.TryParse(value.ToString(), out var dt)) return TimeOnly.FromDateTime(dt);

            throw new ArgumentException("Invalid TimeOnly");
        }
        if (dataTypeId == Ids.DataType.DateTime)
        {
            if (value is DateTime dt) return dt;
            if (DateTime.TryParse(value.ToString(), out var dtt)) return dtt;

            throw new ArgumentException("Invalid DateTime");
        }

        return value;
    }

    private async Task<DomainCrudVersion> GetVersionByCodeAsync(string code, CancellationToken ct)
    {
        var version = await DbContext.DomainCrudVersions
            .Include(s => s.DomainCrudVersionColumns)
            .Where(x => DbContext.DomainCruds.Any(dc => dc.Id == x.CrudId && dc.Code == code))
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        if (version is null)
        {
            throw new InvalidOperationException("No version published for code");
        }

        return version;
    }

    private async Task<Guid> CreateInternalAsync(DomainCrudVersion version, IDictionary<string, object?> data, CancellationToken ct)
    {
        var (api2db, _) = BuildMaps(version);

        var dbType = _configuration.GetValue<string>("DatabaseType") ?? "PostgreSQL";
        var schema = ResolveSchema(version.TenantId);
        string QI(string s) => dbType == "PostgreSQL" ? QuotePg(s) : QuoteMs(s);
        var table = dbType == "PostgreSQL" ? $"{QuotePg(schema)}.{QuotePg(version.TableName)}" : $"[{schema}].[{version.TableName}]";

        var dynDict = version.DomainCrudVersionColumns!.ToDictionary(c => c.PropertyName, StringComparer.OrdinalIgnoreCase);
        var includeProps = data.Keys.Where(k => dynDict.ContainsKey(k)).ToList();

        var newId = GuidGenerator.New();

        var colSql = new StringBuilder();
        var valSql = new StringBuilder();
        var paramNames = new List<string>();
        var paramValues = new List<object?>();
        var p = 0;

        colSql.Append($"{QI(api2db["id"])},{QI(api2db["correlationId"])},{QI(api2db["tenantId"])},{QI(api2db["active"])},{QI(api2db["deleted"])},{QI(api2db["createdBy"])},{QI(api2db["created"])},{QI(api2db["createdTime"])},{QI(api2db["test"])}");

        void AddParam(object? v) { paramNames.Add($"@p{p}"); paramValues.Add(v); p++; }

        AddParam(newId);
        AddParam(CorrelationContext.CorrelationId);
        AddParam(CorrelationContext.TenantId);
        AddParam(true);
        AddParam(false);
        AddParam(CorrelationContext.User?.Id);
        AddParam(false);

        valSql.Append($"{paramNames[0]},{paramNames[1]},{paramNames[2]},{paramNames[3]},{paramNames[4]},{paramNames[5]},{NowDateSql(dbType)},{NowTimeSql(dbType)},{paramNames[6]}");

        for (var i = 0; i < includeProps.Count; i++)
        {
            var prop = includeProps[i];
            var meta = dynDict[prop];
            var norm = NormalizeValue(meta.DataTypeId, data[prop], dbType);

            colSql.Append($",{QI(api2db[prop])}");

            AddParam(norm);

            valSql.Append($",{paramNames[^1]}");
        }

        var sql = $"INSERT INTO {table} ({colSql}) VALUES ({valSql});";

        await ExecuteSqlAsync(sql, paramValues, ct);

        return newId;
    }

    private async Task<int> UpdateInternalAsync(DomainCrudVersion version, Guid id, IDictionary<string, object?> data, CancellationToken ct)
    {
        var (api2db, _) = BuildMaps(version);

        var dbType = _configuration.GetValue<string>("DatabaseType") ?? "PostgreSQL";
        var schema = ResolveSchema(version.TenantId);
        string QI(string s) => dbType == "PostgreSQL" ? QuotePg(s) : QuoteMs(s);
        var table = dbType == "PostgreSQL" ? $"{QuotePg(schema)}.{QuotePg(version.TableName)}" : $"[{schema}].[{version.TableName}]";

        var dynDict = version.DomainCrudVersionColumns!.ToDictionary(c => c.PropertyName, StringComparer.OrdinalIgnoreCase);

        var set = new StringBuilder();
        var parms = new List<object?>
        {
            id,
            CorrelationContext.User?.Id
        };
        var p = 2;

        set.Append($"{QI(api2db["updatedBy"])} = @p1, {QI(api2db["updated"])} = {NowDateSql(dbType)}, {QI(api2db["updatedTime"])} = {NowTimeSql(dbType)}");

        foreach (var kv in data)
        {
            var prop = kv.Key;

            if (!dynDict.TryGetValue(prop, out var meta))
            {
                if (BaseApiProperties.Contains(prop))
                {
                    continue;
                }

                continue;
            }

            var norm = NormalizeValue(meta.DataTypeId, kv.Value, dbType);

            set.Append($", {QI(api2db[prop])} = @p{p}");
            parms.Add(norm);
            p++;
        }

        if (p == 2 && set.Length == 2)
        {
            return 0;
        }

        var sql = $"UPDATE {table} SET {set} WHERE {QI(api2db["id"])} = @p0 AND {QI(api2db["deleted"])} = {(dbType == "PostgreSQL" ? "false" : "0")}";

        var affected = await ExecuteSqlAsync(sql, parms, ct);

        return affected;
    }
}
