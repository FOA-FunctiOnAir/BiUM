using BiUM.Core.Common.Configs;
using BiUM.Core.Database.Native;
using BiUM.Core.Extensions;
using BiUM.Specialized.Database.Native;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public static class NativeDatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddBiUMNativeDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        string name,
        params Assembly[] migrationAssemblies)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (services.Any(descriptor => descriptor.IsKeyedService && Equals(descriptor.ServiceKey, name) && descriptor.ServiceType == typeof(INativeDatabase)))
        {
            return services;
        }

        var databaseOptions = configuration.GetDatabaseOptions(name);

        if (databaseOptions.Type != DatabaseTypes.PostgreSQL)
        {
            throw new NotSupportedException($"Native database {name} supports only {DatabaseTypes.PostgreSQL}; configured type is {databaseOptions.Type}.");
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(configuration.GetRequiredDatabaseConnectionString(name))
        {
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = 100,
            KeepAlive = 30
        };

        var connectionString = connectionStringBuilder.ConnectionString;

        AddNativeDataSource(services, name, connectionString);

        services.AddSingleton(new NativeDatabaseRegistration(name, connectionString));

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(NativeDatabaseInitialiser)))
        {
            services.AddSingleton<NativeDatabaseInitialiser>();
        }

        foreach (var migrationType in migrationAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(NativeSchemaMigration).IsAssignableFrom(type)))
        {
            var migration = (NativeSchemaMigration)Activator.CreateInstance(migrationType)!;

            if (string.Equals(migration.Database, name, StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton(migration);
            }
        }

        services.AddHealthChecks().Add(new Diagnostics.HealthChecks.HealthCheckRegistration(
            $"native-database-{name.ToLowerInvariant()}",
            serviceProvider => new NativeDatabaseHealthCheck(serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(name)),
            failureStatus: null,
            tags: ["ready"]));

        return services;
    }

    public static IServiceCollection AddBiUMReadOnlyNativeDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        string name,
        int maxPoolSize = 10)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (services.Any(descriptor => descriptor.IsKeyedService && Equals(descriptor.ServiceKey, name) && descriptor.ServiceType == typeof(INativeDatabase)))
        {
            return services;
        }

        var databaseOptions = configuration.GetDatabaseOptions(name);

        if (databaseOptions.Type != DatabaseTypes.PostgreSQL)
        {
            throw new NotSupportedException($"Native database {name} supports only {DatabaseTypes.PostgreSQL}; configured type is {databaseOptions.Type}.");
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(configuration.GetRequiredDatabaseConnectionString(name))
        {
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = maxPoolSize,
            KeepAlive = 30
        };

        connectionStringBuilder.Options = string.IsNullOrWhiteSpace(connectionStringBuilder.Options)
            ? ReadOnlySessionOption
            : $"{connectionStringBuilder.Options} {ReadOnlySessionOption}";

        AddNativeDataSource(services, name, connectionStringBuilder.ConnectionString);

        return services;
    }

    private const string ReadOnlySessionOption = "-c default_transaction_read_only=on";

    private static void AddNativeDataSource(IServiceCollection services, string name, string connectionString)
    {
        services.AddKeyedSingleton(name, (serviceProvider, _) =>
            new NpgsqlDataSourceBuilder(connectionString)
                .UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>())
                .Build());

        services.AddKeyedSingleton<INativeDatabase>(name, (serviceProvider, key) =>
            new NpgsqlNativeDatabase(
                (string)key!,
                serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(key),
                serviceProvider.GetRequiredService<ILogger<NpgsqlNativeDatabase>>()));
    }

    public static Task InitialiseNativeDatabases(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var initialiser = serviceProvider.GetService<NativeDatabaseInitialiser>();

        return initialiser is null ? Task.CompletedTask : initialiser.InitialiseAsync(cancellationToken);
    }
}