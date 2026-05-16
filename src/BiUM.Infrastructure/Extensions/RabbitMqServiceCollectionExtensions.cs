using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Core.Compensation;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Compensation;
using BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection;

public static class RabbitMqServiceCollectionExtensions
{
    internal sealed class RabbitMqInfrastructureMarker
    {
    }

    public static IServiceCollection AddBiUMRabbitMqClients(this IServiceCollection services, IConfiguration configuration)
    {
        TryRegisterDefaultBroker(services, configuration);
        MaybeRegisterInfrastructureSharedParts(services);

        return services;
    }

    public static IServiceCollection AddBiUMRabbitMqClients(
        this IServiceCollection services,
        IConfiguration configuration,
        string additionalClientName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(additionalClientName);

        if (string.Equals(additionalClientName, RabbitMqOptions.DefaultClientKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Use AddBiUMRabbitMqClients(configuration) for '{RabbitMqOptions.DefaultClientKey}'. Pass another child key under {RabbitMqOptions.Name}.",
                nameof(additionalClientName));
        }

        TryRegisterDefaultBroker(services, configuration);
        TryRegisterAdditionalBroker(services, configuration, additionalClientName);
        MaybeRegisterInfrastructureSharedParts(services);

        return services;
    }

    private static void TryRegisterDefaultBroker(IServiceCollection services, IConfiguration configuration)
    {
        RegisterOptions(services, configuration, RabbitMqOptions.DefaultClientKey);
        RequireMandatoryDefaultBroker(configuration);

        if (HasUnkeyedService<IRabbitMQClient>(services))
        {
            return;
        }

        services.AddSingleton<IConnectionFactory>(_ =>
            CreateConnectionFactory(
                configuration.BindClientOptionsSnapshot(RabbitMqOptions.DefaultClientKey)));

        services.AddSingleton<RabbitMQConnectionProvider>();
        services.AddSingleton(static sp =>
            new RabbitMQPublisherChannelPool(
                sp.GetRequiredService<RabbitMQConnectionProvider>(),
                sp.GetRequiredService<IOptionsMonitor<RabbitMqOptions>>(),
                RabbitMqOptions.DefaultClientKey));

        services.AddSingleton<IRabbitMQClient>(static sp =>
            new RabbitMQClient(
                sp.GetRequiredService<RabbitMQConnectionProvider>(),
                sp.GetRequiredService<RabbitMQPublisherChannelPool>(),
                sp.GetRequiredService<IRabbitMQSerializer>(),
                sp.GetRequiredService<ICorrelationContextAccessor>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IDateTimeService>(),
                sp.GetRequiredService<IOptions<BiAppOptions>>(),
                sp.GetRequiredService<IOptionsMonitor<RabbitMqOptions>>(),
                RabbitMqOptions.DefaultClientKey,
                sp.GetRequiredService<ILogger<RabbitMQClient>>()));
    }

    private static void TryRegisterAdditionalBroker(
        IServiceCollection services,
        IConfiguration configuration,
        string additionalClientName)
    {
        RegisterOptions(services, configuration, additionalClientName);

        if (!IsBrokerEnabled(configuration, additionalClientName))
        {
            return;
        }

        if (HasKeyedService<IRabbitMQClient>(services, additionalClientName))
        {
            return;
        }

        services.AddKeyedSingleton<IConnectionFactory>(
            additionalClientName,
            static (sp, key) =>
                CreateConnectionFactory(
                    BindClientOptionsSnapshot(sp.GetRequiredService<IConfiguration>(), (string)key!)));

        services.AddKeyedSingleton<RabbitMQConnectionProvider>(
            additionalClientName,
            static (sp, key) =>
                new RabbitMQConnectionProvider(
                    sp.GetRequiredKeyedService<IConnectionFactory>(key),
                    sp.GetRequiredService<IOptions<BiAppOptions>>(),
                    sp.GetRequiredService<ILogger<RabbitMQConnectionProvider>>()));

        services.AddKeyedSingleton<RabbitMQPublisherChannelPool>(
            additionalClientName,
            static (sp, key) =>
                new RabbitMQPublisherChannelPool(
                    sp.GetRequiredKeyedService<RabbitMQConnectionProvider>(key),
                    sp.GetRequiredService<IOptionsMonitor<RabbitMqOptions>>(),
                    (string)key!));

        services.AddKeyedSingleton<IRabbitMQClient>(
            additionalClientName,
            static (sp, key) =>
                new RabbitMQClient(
                    sp.GetRequiredKeyedService<RabbitMQConnectionProvider>(key),
                    sp.GetRequiredKeyedService<RabbitMQPublisherChannelPool>(key),
                    sp.GetRequiredService<IRabbitMQSerializer>(),
                    sp.GetRequiredService<ICorrelationContextAccessor>(),
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    sp.GetRequiredService<IDateTimeService>(),
                    sp.GetRequiredService<IOptions<BiAppOptions>>(),
                    sp.GetRequiredService<IOptionsMonitor<RabbitMqOptions>>(),
                    (string)key!,
                    sp.GetRequiredService<ILogger<RabbitMQClient>>()));
    }

    private static void MaybeRegisterInfrastructureSharedParts(IServiceCollection services)
    {
        if (services.Any(static d =>
                d.ServiceType == typeof(RabbitMqInfrastructureMarker) && !d.IsKeyedService))
        {
            return;
        }

        services.AddSingleton<RabbitMqInfrastructureMarker>();
        services.AddSingleton<IRabbitMQSerializer, RabbitMQSerializer>();

        services.AddScoped<ICompensationSessionFinalizedPublisher, CompensationSessionFinalizedPublisher>();

        services.AddRabbitMQEventHandlers();
        services.AddHostedService<RabbitMQListenerService>();

        services.AddHealthChecks()
            .AddCheck<RabbitMQHealthCheck>("rabbitmq", tags: ["ready"]);
    }

    private static void RegisterOptions(IServiceCollection services, IConfiguration configuration, string clientKey)
    {
        var path = ConfigurationPath.Combine(RabbitMqOptions.Name, clientKey);

        services.Configure<RabbitMqOptions>(clientKey, configuration.GetSection(path));
    }

    private static void RequireMandatoryDefaultBroker(IConfiguration configuration)
    {
        var path = ConfigurationPath.Combine(RabbitMqOptions.Name, RabbitMqOptions.DefaultClientKey);
        var section = configuration.GetSection(path);

        if (!section.Exists())
        {
            throw new InvalidOperationException(
                $"Configuration section '{path}' is required. Move legacy flat {RabbitMqOptions.Name} values under '{path}'.");
        }

        if (!section.GetValue(nameof(RabbitMqOptions.Enable), false))
        {
            throw new InvalidOperationException(
                $"Default RabbitMQ broker must be enabled: set '{path}:{nameof(RabbitMqOptions.Enable)}' to true.");
        }

        var o = configuration.BindClientOptionsSnapshot(RabbitMqOptions.DefaultClientKey);

        if (string.IsNullOrWhiteSpace(o.Hostname))
        {
            throw new InvalidOperationException(
                $"Default RabbitMQ broker requires Hostname: set '{path}:{nameof(RabbitMqOptions.Hostname)}'.");
        }
    }

    private static bool IsBrokerEnabled(IConfiguration configuration, string clientKey)
    {
        var path = ConfigurationPath.Combine(RabbitMqOptions.Name, clientKey);
        var section = configuration.GetSection(path);

        return section.GetValue(nameof(RabbitMqOptions.Enable), false);
    }

    private static RabbitMqOptions BindClientOptionsSnapshot(this IConfiguration configuration, string clientKey)
    {
        var path = ConfigurationPath.Combine(RabbitMqOptions.Name, clientKey);

        return configuration.GetSection(path).Get<RabbitMqOptions>() ?? new RabbitMqOptions();
    }

    internal static ConnectionFactory CreateConnectionFactory(RabbitMqOptions options)
    {
        var recoverySeconds =
            options.NetworkRecoveryIntervalSeconds > 0
                ? options.NetworkRecoveryIntervalSeconds
                : 5;

        var port = options.Port ?? 5672;
        var vhost = string.IsNullOrEmpty(options.VirtualHost) ? "/" : options.VirtualHost;

        return new ConnectionFactory
        {
            Uri = new Uri($"amqp://{options.UserName}:{options.Password}@{options.Hostname}:{port}/{vhost}"),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(recoverySeconds),
            TopologyRecoveryEnabled = true
        };
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