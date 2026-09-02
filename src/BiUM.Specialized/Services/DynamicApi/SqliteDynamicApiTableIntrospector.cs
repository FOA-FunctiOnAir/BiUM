using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

internal sealed class SqliteDynamicApiTableIntrospector : IDynamicApiTableIntrospector
{
    private readonly DbConnection _connection;

    public SqliteDynamicApiTableIntrospector(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<DynamicApiTableReference>> ListCatalogTablesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 'main' AS table_schema, name AS table_name
            FROM sqlite_master
            WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name
            """;

        return await ReadTableListAsync(sql, cancellationToken);
    }

    public async Task<bool> TableExistsAsync(string schema, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 1
            FROM sqlite_master
            WHERE type = 'table' AND name = @table
            LIMIT 1
            """;

        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@table", tableName);

        await EnsureOpenAsync(cancellationToken);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not null;
    }

    public async Task<IReadOnlyList<DynamicApiColumnMetadata>> GetColumnsAsync(
        string schema,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName.Replace("\"", "\"\"")}\");";

        await EnsureOpenAsync(cancellationToken);

        var columns = new List<DynamicApiColumnMetadata>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var columnName = reader.GetString(1);
            var storeType = reader.GetString(2);
            var isNullable = reader.GetInt32(3) == 0;

            columns.Add(new DynamicApiColumnMetadata
            {
                ColumnName = columnName,
                StoreType = storeType,
                IsNullable = isNullable,
                MaxLength = null,
                ClrTypeName = MapSqlite(storeType)
            });
        }

        return columns;
    }

    private async Task<IReadOnlyList<DynamicApiTableReference>> ReadTableListAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        var tables = new List<DynamicApiTableReference>();

        await using var command = _connection.CreateCommand();
        command.CommandText = sql;

        await EnsureOpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);

            if (DynamicApiSchemaRules.IsTableBlocked(schema, table))
            {
                continue;
            }

            tables.Add(new DynamicApiTableReference(schema, table));
        }

        return tables;
    }

    private async Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }
    }

    private static void AddParameter(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string MapSqlite(string storeType)
    {
        return storeType.ToUpperInvariant() switch
        {
            "INTEGER" => "long",
            "REAL" => "double",
            "BLOB" => "byte[]",
            _ => "string"
        };
    }
}