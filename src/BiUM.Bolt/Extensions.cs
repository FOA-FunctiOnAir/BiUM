using BiUM.Bolt.Database;
using BiUM.Core.Common.Configs;
using BiUM.Specialized.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ConfigureApp
{
    public static IServiceCollection AddBolt<TDbContext, TDbContextInitialiser>(
        this IServiceCollection services,
        IConfiguration configuration
    )
        where TDbContext : DbContext
        where TDbContextInitialiser : class
    {
        services.Configure<BoltOptions>(configuration.GetSection(BoltOptions.Name));

        var serviceProvider = services.BuildServiceProvider();
        var boltOptions = serviceProvider.GetRequiredService<IOptions<BoltOptions>>();

        if (configuration.GetValue<string>("DatabaseType") == "PostgreSQL")
        {
            var databaseName = string.Empty;
            var connectionStringArray = configuration.GetConnectionString("PostgreSQL")?.Split(";");

            if (connectionStringArray is null || connectionStringArray.Length == 0)
            {
                return services;
            }

            foreach (var connectionStringItem in connectionStringArray)
            {
                var connectionStringItems = connectionStringItem.Trim().Split("=");

                if (connectionStringItems[0] == "Database")
                {
                    databaseName = connectionStringItems[1].Trim();

                    break;
                }
            }

            var boltDbName = databaseName ?? "db";

            var connectionString = string.Format(boltOptions.Value.ConnectionString, boltDbName);

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
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null);
                    }));
        }

        services.AddScoped(typeof(IBaseBoltDbContextInitialiser), typeof(TDbContextInitialiser));
        services.AddScoped<BoltEntitySaveChangesInterceptor>();

        services.AddHealthChecks().AddDbContextCheck<TDbContext>();

        return services;
    }
}