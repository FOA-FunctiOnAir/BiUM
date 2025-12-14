using BiUM.Core.Caching.Redis;
using BiUM.Core.Common.Configs;
using BiUM.Core.Logging.Serilog;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Infrastructure.Services.Caching.Redis;
using BiUM.Infrastructure.Services.Logging.Serilog;
using BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IHostBuilder host, IConfiguration configuration)
    {
        // Configure Grpc
        services.AddGrpc();
        services.AddGrpcReflection();

        // Configure Redis
        services.AddRedisServices(configuration);

        // Configure RabbitMQ
        services.AddRabbitMQServices(configuration);

        // Configure RabbitMQ
        services.AddSerilogServices(host, configuration);

        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddRabbitMQServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMQOptions>(configuration.GetSection(RabbitMQOptions.Name));
        services.AddSingleton<IRabbitMQClient, RabbitMQClient>();
        services.AddHostedService<RabbitMQListenerService>();
        services.AddRabbitMQEventHandlers();

        return services;
    }

    private static IServiceCollection AddRedisServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisClientOptions>(configuration.GetSection(RedisClientOptions.Name));
        services.AddSingleton<IRedisClient, RedisClient>();

        return services;
    }

    private static IServiceCollection AddSerilogServices(this IServiceCollection services, IHostBuilder host, IConfiguration configuration)
    {
        services.Configure<SerilogOptions>(configuration.GetSection(SerilogOptions.Name));

        var minimumLevel = configuration.GetValue<string>("SerilogOptions:MinimumLevel");

        if (!Enum.TryParse<LogEventLevel>(minimumLevel, out var level))
        {
            level = LogEventLevel.Information;
        }

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .Enrich.FromLogContext()
            .WriteTo.Console(new JsonFormatter())
            .CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(logger, dispose: true);
        });

        services.AddScoped<ISerilogClient, SerilogClient>();

        host.UseSerilog(logger);

        return services;
    }
}