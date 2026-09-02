using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

internal sealed class SqlServerDynamicApiTableIntrospector : IDynamicApiTableIntrospector
{
    private readonly DbConnection _connection;

    public SqlServerDynamicApiTableIntrospector(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<DynamicApiTableReference>> ListCatalogTablesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND TABLE_SCHEMA = 'dbo'
            ORDER BY TABLE_NAME
            """;

        return await ReadTableListAsync(sql, cancellationToken);
    }

    public async Task<bool> TableExistsAsync(string schema, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP 1 1
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
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
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
            ORDER BY ORDINAL_POSITION
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
            var isNullable = string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase);
            int? maxLength = reader.IsDBNull(3) ? null : reader.GetInt32(3);

            columns.Add(new DynamicApiColumnMetadata
            {
                ColumnName = columnName,
                StoreType = dataType,
                IsNullable = isNullable,
                MaxLength = maxLength,
                ClrTypeName = DynamicApiColumnTypeMapper.MapSqlServer(dataType)
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