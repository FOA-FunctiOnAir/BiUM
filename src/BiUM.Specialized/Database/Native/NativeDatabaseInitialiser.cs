using BiUM.Core.Database.Native;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database.Native;

internal sealed class NativeDatabaseInitialiser
{
    private const string HistoryTable = "\"public\".\"__NATIVE_MIGRATION_HISTORY\"";

    private readonly IEnumerable<NativeDatabaseRegistration> _registrations;
    private readonly IEnumerable<NativeSchemaMigration> _migrations;
    private readonly ILogger<NativeDatabaseInitialiser> _logger;

    public NativeDatabaseInitialiser(
        IEnumerable<NativeDatabaseRegistration> registrations,
        IEnumerable<NativeSchemaMigration> migrations,
        ILogger<NativeDatabaseInitialiser> logger)
    {
        _registrations = registrations;
        _migrations = migrations;
        _logger = logger;
    }

    public async Task InitialiseAsync(CancellationToken cancellationToken)
    {
        foreach (var registration in _registrations)
        {
            await EnsureDatabaseAsync(registration, cancellationToken);
            await ApplyMigrationsAsync(registration, cancellationToken);
        }
    }

    private async Task EnsureDatabaseAsync(NativeDatabaseRegistration registration, CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(registration.ConnectionString);
        var databaseName = builder.Database;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException($"Native database {registration.Name} connection string has no database name.");
        }

        builder.Database = "postgres";
        builder.Pooling = false;

        await using var connection = new NpgsqlConnection(builder.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using (var existsCommand = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection))
        {
            existsCommand.Parameters.AddWithValue("name", databaseName);

            if (await existsCommand.ExecuteScalarAsync(cancellationToken) is not null)
            {
                return;
            }
        }

        try
        {
            await using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"", StringComparison.Ordinal)}\"", connection);

            await createCommand.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Native database {Database} created", databaseName);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateDatabase)
        {
            _logger.LogInformation("Native database {Database} already created by another instance", databaseName);
        }
    }

    private async Task ApplyMigrationsAsync(NativeDatabaseRegistration registration, CancellationToken cancellationToken)
    {
        var migrations = _migrations
            .Where(migration => string.Equals(migration.Database, registration.Name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(migration => migration.Id, StringComparer.Ordinal)
            .ToList();

        if (migrations.Count == 0)
        {
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(registration.ConnectionString)
        {
            Pooling = false
        };

        await using var connection = new NpgsqlConnection(builder.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_lock(hashtext('__native_migration'))", connection))
        {
            await lockCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        try
        {
            await using (var historyCommand = new NpgsqlCommand($"CREATE TABLE IF NOT EXISTS {HistoryTable} (\"ID\" text PRIMARY KEY, \"APPLIED_AT\" timestamptz NOT NULL DEFAULT now())", connection))
            {
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var applied = new HashSet<string>(StringComparer.Ordinal);

            await using (var appliedCommand = new NpgsqlCommand($"SELECT \"ID\" FROM {HistoryTable}", connection))
            await using (var reader = await appliedCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    applied.Add(reader.GetString(0));
                }
            }

            foreach (var migration in migrations.Where(migration => !applied.Contains(migration.Id)))
            {
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                await using (var migrationCommand = new NpgsqlCommand(migration.Sql, connection, transaction))
                {
                    await migrationCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var insertCommand = new NpgsqlCommand($"INSERT INTO {HistoryTable} (\"ID\") VALUES (@id)", connection, transaction))
                {
                    insertCommand.Parameters.AddWithValue("id", migration.Id);

                    await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Native migration {MigrationId} applied to {Database}", migration.Id, registration.Name);
            }
        }
        finally
        {
            await using var unlockCommand = new NpgsqlCommand("SELECT pg_advisory_unlock(hashtext('__native_migration'))", connection);

            await unlockCommand.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }
}

internal sealed record NativeDatabaseRegistration(string Name, string ConnectionString);