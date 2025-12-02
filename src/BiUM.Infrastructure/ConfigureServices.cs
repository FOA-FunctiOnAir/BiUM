using BiUM.Core.Caching.Redis;
using BiUM.Core.Common.Configs;
using BiUM.Core.Logging.Serilog;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Infrastructure.Common.Configs;
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
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IHostBuilder host, IConfiguration configuration, Specialized specialized = null)
    {
        if (specialized == null)
        {
            // Load settings from appsettings.json
            specialized = new Specialized();
            configuration.Bind(specialized);
        }

        // Configure Grpc
        services.AddGrpc();
        services.AddGrpcReflection();

        // Configure Redis
        services.AddSingleton(specialized.RedisClientOptions);
        services.AddSingleton<IRedisClient, RedisClient>();

        // Configure RabbitMQ
        services.AddRabbitMQServices(configuration);

        if (!Enum.TryParse<LogEventLevel>(specialized.SerilogOptions.MinimumLevel, out var level))
        {
            level = LogEventLevel.Information;
        }

        // TODO: Serilog getting Exception
        // Configure Serilog
        services.AddSingleton(specialized.SerilogOptions);
        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .Enrich.FromLogContext()
            .WriteTo.Console(new JsonFormatter())
            .CreateLogger();

        //foreach (var writeTo in specialized.SerilogOptions.WriteTo)
        //{
        //    if (writeTo.Name == "Console")
        //    {
        //        logger = new LoggerConfiguration()
        //            .MinimumLevel.Is(Enum.Parse<LogEventLevel>(specialized.SerilogOptions.MinimumLevel))
        //            .CreateLogger();
        //    }
        //    else if (writeTo.Name == "File")
        //    {
        //        logger.WriteTo.File(writeTo.Args["path"], rollingInterval: Enum.Parse<RollingInterval>(writeTo.Args["rollingInterval"]));

        //        logger = new LoggerConfiguration()
        //            .MinimumLevel.Is(Enum.Parse<LogEventLevel>(specialized.SerilogOptions.MinimumLevel))
        //            .WriteTo.File(writeTo.Args["path"], rollingInterval: Enum.Parse<RollingInterval>(writeTo.Args["rollingInterval"]))
        //            .CreateLogger();
        //    }
        //}

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(logger, dispose: true);
        });

        services.AddScoped<ISerilogClient, SerilogClient>();

        host.UseSerilog(logger);

        services.AddHealthChecks();

        // services.AddOptions<RedisClientOptions>();
        // services.Configure<RedisClientOptions>(configuration.GetSection("RedisClientOptions"));
        // services.AddDistributedMemoryCache(options =>
        // {
        //     configuration.GetSection("RedisClientOptions").Bind(options);
        // });
        // services.AddScoped<IRedisClient, RedisClient>();

        // // Add RabbitMQ Options
        // services.AddOptions<RabbitMQOptions>();
        // services.Configure<RabbitMQOptions>(configuration.GetSection("RabbitMQOptions"));
        // services.AddOptions<RabbitMQOptions>()
        //     .Configure<IConfiguration>((options, configuration) =>
        //     {
        //         configuration.GetSection(RabbitMQOptions.Name).Bind(options);
        //     });
        // services.AddScoped<IRabbitMQClient, RabbitMQClient>();

        // // Add Serilog logging
        //services.AddLogging(loggingBuilder =>
        //{
        //    loggingBuilder.ClearProviders();
        //    loggingBuilder.AddSerilog(dispose: true);
        //});
        //services.AddScoped<ISerilogClient, SerilogClient>();

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
}