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
        if (configuration.GetValue<string>("DatabaseType") == "PostgreSQL")
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
        else if (configuration.GetValue<string>("DatabaseType") == "MSSQL")
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
        var databaseType = configuration.GetValue<string>("DatabaseType");

        if (databaseType == "PostgreSQL")
        {
            var connectionStringArray = configuration.GetConnectionString("PostgreSQL")?.Split(";");

            if (connectionStringArray is null || connectionStringArray.Length == 0)
            {
                return null;
            }

            foreach (var connectionStringItem in connectionStringArray)
            {
                var connectionStringItems = connectionStringItem.Trim().Split("=");

                if (connectionStringItems.Length == 2 && connectionStringItems[0].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase))
                {
                    return connectionStringItems[1].Trim();
                }
            }

            return null;
        }

        if (databaseType == "MSSQL")
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(configuration.GetConnectionString("MSSQL"));

            return string.IsNullOrWhiteSpace(builder.InitialCatalog) ? null : builder.InitialCatalog;
        }

        return null;
    }
}