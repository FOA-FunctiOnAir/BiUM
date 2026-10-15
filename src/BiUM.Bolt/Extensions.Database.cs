using BiUM.Core.Common.Configs;
using BiUM.Core.Extensions;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class BoltDatabaseExtensions
{
    internal static void AddBoltDbContext<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
        where TDbContext : DbContext
    {
        var databaseType = configuration.GetDatabaseType();

        if (databaseType == DatabaseTypes.PostgreSQL)
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Pooling = true,
                MinPoolSize = 0,
                MaxPoolSize = 100,
                KeepAlive = 30
            };

            services.AddDbContext<TDbContext>(options =>
                options.UseNpgsql(
                    connectionStringBuilder.ConnectionString,
                    npgsqlOptions =>
                    {
                        var boltMigrationsAssembly = EfMigrationsAssemblyResolver.GetActiveMigrationsAssemblyName(configuration, bolt: true);

                        if (boltMigrationsAssembly is not null)
                        {
                            _ = npgsqlOptions.MigrationsAssembly(boltMigrationsAssembly);
                        }

                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null);
                    }));
        }
        else if (databaseType == DatabaseTypes.MSSQL)
        {
            services.AddDbContext<TDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sql =>
                    {
                        var boltMigrationsAssembly = EfMigrationsAssemblyResolver.GetActiveMigrationsAssemblyName(configuration, bolt: true);

                        if (boltMigrationsAssembly is not null)
                        {
                            _ = sql.MigrationsAssembly(boltMigrationsAssembly);
                        }

                        _ = sql.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    }));
        }
    }

    internal static string? TryGetMainDatabaseName(IConfiguration configuration)
    {
        var databaseOptions = configuration.GetDatabaseOptions();

        if (string.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
        {
            return null;
        }

        if (databaseOptions.Type == DatabaseTypes.PostgreSQL)
        {
            var builder = new NpgsqlConnectionStringBuilder(databaseOptions.ConnectionString);

            return string.IsNullOrWhiteSpace(builder.Database) ? null : builder.Database;
        }

        if (databaseOptions.Type == DatabaseTypes.MSSQL)
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(databaseOptions.ConnectionString);

            return string.IsNullOrWhiteSpace(builder.InitialCatalog) ? null : builder.InitialCatalog;
        }

        return null;
    }
}