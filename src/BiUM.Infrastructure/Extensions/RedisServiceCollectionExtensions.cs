using BiUM.Core.Caching.Redis;
using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Caching.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection;

public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddBiUMRedisClients(this IServiceCollection services, IConfiguration configuration)
    {
        TryRegisterDefaultRedisClient(services, configuration);

        return services;
    }

    public static IServiceCollection AddBiUMRedisClients(
        this IServiceCollection services,
        IConfiguration configuration,
        string additionalClientName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(additionalClientName);

        if (string.Equals(additionalClientName, RedisOptions.DefaultClientKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Use AddBiUMRedisClients(configuration) for '{RedisOptions.DefaultClientKey}'. Pass another child key under {RedisOptions.Name}.",
                nameof(additionalClientName));
        }

        TryRegisterDefaultRedisClient(services, configuration);
        TryRegisterAdditionalRedisClient(services, configuration, additionalClientName);

        return services;
    }

    private static void TryRegisterDefaultRedisClient(IServiceCollection services, IConfiguration configuration)
    {
        RegisterOptions(services, configuration, RedisOptions.DefaultClientKey);

        if (!IsRedisOptionsEnabled(configuration, RedisOptions.DefaultClientKey))
        {
            return;
        }

        if (HasUnkeyedService<IRedisClient>(services))
        {
            return;
        }

        services.AddSingleton<IRedisClient>(static sp =>
            new RedisClient(
                sp.GetRequiredService<IOptionsMonitor<RedisOptions>>(),
                RedisOptions.DefaultClientKey,
                sp.GetRequiredService<IDateTimeService>(),
                sp.GetRequiredService<ILogger<RedisClient>>()));
    }

    private static void TryRegisterAdditionalRedisClient(
        IServiceCollection services,
        IConfiguration configuration,
        string additionalClientName)
    {
        RegisterOptions(services, configuration, additionalClientName);

        if (!IsRedisOptionsEnabled(configuration, additionalClientName))
        {
            return;
        }

        if (HasKeyedService<IRedisClient>(services, additionalClientName))
        {
            return;
        }

        services.AddKeyedSingleton<IRedisClient>(
            additionalClientName,
            (Func<IServiceProvider, object?, IRedisClient>)((sp, _) =>
                new RedisClient(
                    sp.GetRequiredService<IOptionsMonitor<RedisOptions>>(),
                    additionalClientName,
                    sp.GetRequiredService<IDateTimeService>(),
                    sp.GetRequiredService<ILogger<RedisClient>>())));
    }

    private static bool IsRedisOptionsEnabled(IConfiguration configuration, string clientKey)
    {
        var path = ConfigurationPath.Combine(RedisOptions.Name, clientKey);
        var section = configuration.GetSection(path);

        return section.GetValue(nameof(RedisOptions.Enable), false);
    }

    private static void RegisterOptions(IServiceCollection services, IConfiguration configuration, string clientKey)
    {
        var path = ConfigurationPath.Combine(RedisOptions.Name, clientKey);

        services.Configure<RedisOptions>(clientKey, configuration.GetSection(path));
    }

    private static bool HasUnkeyedService<TService>(IServiceCollection services) =>
        services.Any(static d =>
            d.ServiceType == typeof(TService)
            && !d.IsKeyedService);

    private static bool HasKeyedService<TService>(IServiceCollection services, object serviceKey)
    {
        foreach (var d in services)
        {
            if (d.ServiceType != typeof(TService) || !d.IsKeyedService)
            {
                continue;
            }

            if (KeyedServiceMatches(d.ServiceKey, serviceKey))
            {
                return true;
            }
        }

        return false;
    }

    private static bool KeyedServiceMatches(object? registeredKey, object requestedKey)
    {
        if (registeredKey is string rs && requestedKey is string rq)
        {
            return string.Equals(rs, rq, StringComparison.Ordinal);
        }

        return Equals(registeredKey, requestedKey);
    }
}