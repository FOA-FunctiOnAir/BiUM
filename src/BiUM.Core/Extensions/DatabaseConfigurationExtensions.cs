using BiUM.Core.Common.Configs;
using Microsoft.Extensions.Configuration;
using System;

namespace BiUM.Core.Extensions;

public static class DatabaseConfigurationExtensions
{
    public static DatabaseOptions GetDatabaseOptions(this IConfiguration configuration, string name = DatabaseOptions.DefaultKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var options = new DatabaseOptions();

        configuration.GetSection(ConfigurationPath.Combine(DatabaseOptions.Name, name)).Bind(options);

        return options;
    }

    public static string GetDatabaseType(this IConfiguration configuration, string name = DatabaseOptions.DefaultKey)
    {
        return configuration.GetDatabaseOptions(name).Type;
    }

    public static string GetRequiredDatabaseConnectionString(this IConfiguration configuration, string name = DatabaseOptions.DefaultKey)
    {
        var connectionString = configuration.GetDatabaseOptions(name).ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{DatabaseOptions.Name}:{name}:ConnectionString is required.");
        }

        return connectionString;
    }
}