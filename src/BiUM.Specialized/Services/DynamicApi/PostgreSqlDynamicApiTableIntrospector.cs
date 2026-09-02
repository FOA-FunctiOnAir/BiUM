using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

internal sealed class PostgreSqlDynamicApiTableIntrospector : IDynamicApiTableIntrospector
{
    private readonly DbConnection _connection;

    public PostgreSqlDynamicApiTableIntrospector(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<DynamicApiTableReference>> ListCatalogTablesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT table_schema, table_name
            FROM information_schema.tables
            WHERE table_type = 'BASE TABLE'
              AND table_schema = 'public'
            ORDER BY table_name
            """;

        return await ReadTableListAsync(sql, cancellationToken);
    }

    public async Task<bool> TableExistsAsync(string schema, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = @schema AND table_name = @table
            LIMIT 1
            """;

        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@schema", schema);
        AddParameter(command, "@table", tableName);

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not null;
    }

    public async Task<IReadOnlyList<DynamicApiColumnMetadata>> GetColumnsAsync(
        string schema,
        string tableName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT column_name, data_type, udt_name, is_nullable, character_maximum_length
            FROM information_schema.columns
            WHERE table_schema = @schema AND table_name = @table
            ORDER BY ordinal_position
            """;

        var columns = new List<DynamicApiColumnMetadata>();

        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@schema", schema);
        AddParameter(command, "@table", tableName);

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            var udtName = reader.IsDBNull(2) ? null : reader.GetString(2);
            var isNullable = string.Equals(reader.GetString(3), "YES", StringComparison.OrdinalIgnoreCase);
            int? maxLength = reader.IsDBNull(4) ? null : reader.GetInt32(4);

            columns.Add(new DynamicApiColumnMetadata
            {
                ColumnName = columnName,
                StoreType = udtName ?? dataType,
                IsNullable = isNullable,
                MaxLength = maxLength,
                ClrTypeName = DynamicApiColumnTypeMapper.MapPostgreSql(dataType, udtName)
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

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

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

    private static void AddParameter(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}