using BiUM.Core.Database.Native;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database.Native;

internal sealed class NpgsqlNativeDatabase : INativeDatabase
{
    private static readonly TimeSpan SlowCommandThreshold = TimeSpan.FromSeconds(2);

    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<NpgsqlNativeDatabase> _logger;

    public NpgsqlNativeDatabase(string name, NpgsqlDataSource dataSource, ILogger<NpgsqlNativeDatabase> logger)
    {
        Name = name;
        _dataSource = dataSource;
        _logger = logger;
    }

    public string Name { get; }

    public async Task<IReadOnlyList<TResult>> QueryAsync<TResult>(NativeSqlQuery<TResult> query, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        await using var command = CreateCommand(query);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<TResult>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(query.Map(reader));
        }

        LogIfSlow(query.GetType().Name, stopwatch.Elapsed);

        return results;
    }

    public async Task<int> ExecuteAsync(NativeSqlCommand command, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        await using var npgsqlCommand = CreateCommand(command);

        var affected = await npgsqlCommand.ExecuteNonQueryAsync(cancellationToken);

        LogIfSlow(command.GetType().Name, stopwatch.Elapsed);

        return affected;
    }

    public async Task<int> BulkMergeAsync<TRow>(NativeBulkMerge<TRow> merge, IReadOnlyCollection<TRow> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return 0;
        }

        var stopwatch = Stopwatch.StartNew();

        var staging = $"__bulk_{Guid.NewGuid():N}";

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var createCommand = new NpgsqlCommand(BuildCreateStagingSql(merge, staging), connection, transaction))
        {
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var writer = await connection.BeginBinaryImportAsync(BuildCopySql(merge, staging), cancellationToken))
        {
            foreach (var row in rows)
            {
                await writer.StartRowAsync(cancellationToken);

                foreach (var column in merge.Columns)
                {
                    var value = column.Value(row);

                    if (value is null)
                    {
                        await writer.WriteNullAsync(cancellationToken);
                    }
                    else
                    {
                        await writer.WriteAsync(value, ToNpgsqlDbType(column.Type), cancellationToken);
                    }
                }
            }

            await writer.CompleteAsync(cancellationToken);
        }

        int affected;

        await using (var mergeCommand = new NpgsqlCommand(BuildMergeSql(merge, staging), connection, transaction))
        {
            affected = await mergeCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        LogIfSlow(merge.GetType().Name, stopwatch.Elapsed);

        return affected;
    }

    public async Task<int> EnsureRangePartitionsAsync(NativeRangePartitionSet partitionSet, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var periodStart = FloorToInterval(ToUtc(from), partitionSet.Interval);
        var end = ToUtc(to);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtext(@key))", connection, transaction))
        {
            lockCommand.Parameters.AddWithValue("key", $"{partitionSet.Schema}.{partitionSet.ParentTable}");

            await lockCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var created = 0;

        while (periodStart <= end)
        {
            var periodEnd = AddInterval(periodStart, partitionSet.Interval);

            await using var createCommand = new NpgsqlCommand(BuildCreatePartitionSql(partitionSet, periodStart), connection, transaction);

            await createCommand.ExecuteNonQueryAsync(cancellationToken);

            created++;
            periodStart = periodEnd;
        }

        await transaction.CommitAsync(cancellationToken);

        return created;
    }

    public async Task<int> DropRangePartitionsBeforeAsync(NativeRangePartitionSet partitionSet, DateTime before, CancellationToken cancellationToken)
    {
        var threshold = ToUtc(before);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        var partitionNames = new List<string>();

        await using (var listCommand = new NpgsqlCommand(
            "SELECT child.relname FROM pg_inherits JOIN pg_class parent ON parent.oid = pg_inherits.inhparent JOIN pg_class child ON child.oid = pg_inherits.inhrelid JOIN pg_namespace ns ON ns.oid = parent.relnamespace WHERE parent.relname = @parent AND ns.nspname = @schema",
            connection))
        {
            listCommand.Parameters.AddWithValue("parent", partitionSet.ParentTable);
            listCommand.Parameters.AddWithValue("schema", partitionSet.Schema);

            await using var reader = await listCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                partitionNames.Add(reader.GetString(0));
            }
        }

        var dropped = 0;

        foreach (var partitionName in partitionNames)
        {
            var periodStart = TryParsePartitionStart(partitionSet, partitionName);

            if (!periodStart.HasValue || AddInterval(periodStart.Value, partitionSet.Interval) > threshold)
            {
                continue;
            }

            await using var dropCommand = new NpgsqlCommand($"DROP TABLE IF EXISTS {Quote(partitionSet.Schema)}.{Quote(partitionName)}", connection);

            await dropCommand.ExecuteNonQueryAsync(cancellationToken);

            dropped++;
        }

        return dropped;
    }

    private NpgsqlCommand CreateCommand(NativeSqlStatement statement)
    {
        var command = _dataSource.CreateCommand(statement.Sql);

        if (statement.CommandTimeoutSeconds.HasValue)
        {
            command.CommandTimeout = statement.CommandTimeoutSeconds.Value;
        }

        foreach (var parameter in statement.GetParameters())
        {
            var npgsqlParameter = parameter.Type.HasValue
                ? new NpgsqlParameter(parameter.Name, ToNpgsqlDbType(parameter.Type.Value)) { Value = parameter.Value ?? DBNull.Value }
                : new NpgsqlParameter(parameter.Name, parameter.Value ?? DBNull.Value);

            command.Parameters.Add(npgsqlParameter);
        }

        return command;
    }

    private void LogIfSlow(string statementName, TimeSpan elapsed)
    {
        if (elapsed >= SlowCommandThreshold)
        {
            _logger.LogWarning("Native statement {Statement} on {Database} took {ElapsedMs} ms", statementName, Name, (long)elapsed.TotalMilliseconds);
        }
    }

    internal static string BuildCreateStagingSql<TRow>(NativeBulkMerge<TRow> merge, string staging)
    {
        return $"CREATE TEMP TABLE {Quote(staging)} (LIKE {Quote(merge.Schema)}.{Quote(merge.Table)} INCLUDING DEFAULTS) ON COMMIT DROP";
    }

    internal static string BuildCopySql<TRow>(NativeBulkMerge<TRow> merge, string staging)
    {
        return $"COPY {Quote(staging)} ({BuildColumnList(merge)}) FROM STDIN (FORMAT BINARY)";
    }

    internal static string BuildMergeSql<TRow>(NativeBulkMerge<TRow> merge, string staging)
    {
        var columnList = BuildColumnList(merge);
        var conflictList = string.Join(", ", merge.ConflictColumns.Select(Quote));
        var conflictClause = merge.ConflictAction == NativeBulkConflictAction.DoUpdate
            ? BuildDoUpdateClause(merge)
            : "DO NOTHING";

        return $"INSERT INTO {Quote(merge.Schema)}.{Quote(merge.Table)} ({columnList}) SELECT {columnList} FROM {Quote(staging)} ON CONFLICT ({conflictList}) {conflictClause}";
    }

    internal static string BuildCreatePartitionSql(NativeRangePartitionSet partitionSet, DateTime periodStart)
    {
        var periodEnd = AddInterval(periodStart, partitionSet.Interval);

        return $"CREATE TABLE IF NOT EXISTS {Quote(partitionSet.Schema)}.{Quote(GetPartitionName(partitionSet, periodStart))} PARTITION OF {Quote(partitionSet.Schema)}.{Quote(partitionSet.ParentTable)} FOR VALUES FROM ('{FormatTimestamp(periodStart)}') TO ('{FormatTimestamp(periodEnd)}')";
    }

    private static string BuildColumnList<TRow>(NativeBulkMerge<TRow> merge)
    {
        return string.Join(", ", merge.Columns.Select(column => Quote(column.Name)));
    }

    private static string BuildDoUpdateClause<TRow>(NativeBulkMerge<TRow> merge)
    {
        var updates = merge.Columns
            .Where(column => !merge.ConflictColumns.Contains(column.Name, StringComparer.Ordinal))
            .Select(column => $"{Quote(column.Name)} = EXCLUDED.{Quote(column.Name)}")
            .ToList();

        return updates.Count == 0 ? "DO NOTHING" : $"DO UPDATE SET {string.Join(", ", updates)}";
    }

    internal static string GetPartitionName(NativeRangePartitionSet partitionSet, DateTime periodStart)
    {
        return $"{partitionSet.ParentTable}_{periodStart.ToString(GetSuffixFormat(partitionSet.Interval), CultureInfo.InvariantCulture)}";
    }

    internal static DateTime? TryParsePartitionStart(NativeRangePartitionSet partitionSet, string partitionName)
    {
        var prefix = $"{partitionSet.ParentTable}_";

        if (!partitionName.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var suffix = partitionName[prefix.Length..];

        return DateTime.TryParseExact(suffix, GetSuffixFormat(partitionSet.Interval), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var periodStart)
            ? DateTime.SpecifyKind(periodStart, DateTimeKind.Utc)
            : null;
    }

    private static string GetSuffixFormat(NativePartitionInterval interval) => interval switch
    {
        NativePartitionInterval.Day => "yyyyMMdd",
        NativePartitionInterval.Month => "yyyyMM",
        _ => "yyyy"
    };

    internal static DateTime FloorToInterval(DateTime value, NativePartitionInterval interval) => interval switch
    {
        NativePartitionInterval.Day => new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Utc),
        NativePartitionInterval.Month => new DateTime(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Utc),
        _ => new DateTime(value.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    internal static DateTime AddInterval(DateTime value, NativePartitionInterval interval) => interval switch
    {
        NativePartitionInterval.Day => value.AddDays(1),
        NativePartitionInterval.Month => value.AddMonths(1),
        _ => value.AddYears(1)
    };

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string FormatTimestamp(DateTime value) => value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    internal static NpgsqlDbType ToNpgsqlDbType(NativeColumnType type) => type switch
    {
        NativeColumnType.Boolean => NpgsqlDbType.Boolean,
        NativeColumnType.Int16 => NpgsqlDbType.Smallint,
        NativeColumnType.Int32 => NpgsqlDbType.Integer,
        NativeColumnType.Int64 => NpgsqlDbType.Bigint,
        NativeColumnType.Double => NpgsqlDbType.Double,
        NativeColumnType.Numeric => NpgsqlDbType.Numeric,
        NativeColumnType.Text => NpgsqlDbType.Text,
        NativeColumnType.Uuid => NpgsqlDbType.Uuid,
        NativeColumnType.TimestampTz => NpgsqlDbType.TimestampTz,
        NativeColumnType.Date => NpgsqlDbType.Date,
        NativeColumnType.Int16Array => NpgsqlDbType.Array | NpgsqlDbType.Smallint,
        NativeColumnType.Int32Array => NpgsqlDbType.Array | NpgsqlDbType.Integer,
        NativeColumnType.Int64Array => NpgsqlDbType.Array | NpgsqlDbType.Bigint,
        NativeColumnType.DoubleArray => NpgsqlDbType.Array | NpgsqlDbType.Double,
        NativeColumnType.UuidArray => NpgsqlDbType.Array | NpgsqlDbType.Uuid,
        NativeColumnType.TextArray => NpgsqlDbType.Array | NpgsqlDbType.Text,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}