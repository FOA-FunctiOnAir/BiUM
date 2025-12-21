using BiUM.Core.Authorization;
using BiUM.Core.Caching.Redis;
using BiUM.Core.Common.Configs;
using BiUM.Core.File;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Core.Serialization;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Infrastructure.Services.Caching.Redis;
using BiUM.Infrastructure.Services.File;
using BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;
using BiUM.Specialized.Services.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleHtmlToPdf;
using SimpleHtmlToPdf.Interfaces;
using SimpleHtmlToPdf.UnmanagedHandler;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    private static readonly TimeSpan FastRedisDurationLimit = TimeSpan.FromMilliseconds(100);

    public static WebApplicationBuilder ConfigureInfrastructureServices(this WebApplicationBuilder builder)
    {
        var appOptionsSection = builder.Configuration.GetSection(BiAppOptions.Name);

        builder.Services.Configure<BiAppOptions>(appOptionsSection);

        var appOptions = appOptionsSection.Get<BiAppOptions>();

        builder.Services.Configure<HttpClientsOptions>(builder.Configuration.GetSection(HttpClientsOptions.Name));

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddHttpClient();

        builder.Services.AddHealthChecks();

        var serviceName = $"BiApp.{appOptions?.Domain ?? "Unknown"}";

        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
            logging.AddOtlpExporter();
            logging.AddConsoleExporter(builder.Environment);
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddOtlpExporter()
                .AddConsoleExporter(builder.Configuration, builder.Environment))
            .WithTracing(tracing => tracing
                .AddSource(serviceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity.SetTag("db.name", command.Connection?.Database);
                        activity.SetTag("activity.duration", activity.Duration.TotalMilliseconds);
                    };
                })
                .AddRedisInstrumentation(options =>
                    options.Enrich = (activity, context) =>
                    {
                        activity.SetTag("redis.duration", context.ProfiledCommand.ElapsedTime.TotalMilliseconds);

                        if (context.ProfiledCommand.ElapsedTime < FastRedisDurationLimit)
                        {
                            activity.SetTag("redis.is_fast", true);
                        }
                    })
                .AddOtlpExporter()
                .AddConsoleExporter(builder.Configuration, builder.Environment));

        builder.Services.AddEndpointsApiExplorer();

        if (appOptions is not null)
        {
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(
                    appOptions.DomainVersion,
                    new OpenApiInfo
                    {
                        Title = $"BiApp {appOptions.Domain} APIs",
                        Version = appOptions.DomainVersion
                    });
            });
        }

        // Health Checks
        builder.Services.AddHealthChecks()
            .AddApplicationLifecycleHealthCheck()
            .AddManualHealthCheck()
            .AddResourceUtilizationHealthCheck(o =>
            {
                o.CpuThresholds = new ResourceUsageThresholds
                {
                    DegradedUtilizationPercentage = 85, UnhealthyUtilizationPercentage = 95,
                };
                o.MemoryThresholds = new ResourceUsageThresholds
                {
                    DegradedUtilizationPercentage = 85, UnhealthyUtilizationPercentage = 95,
                };
            })
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

        builder.Services.AddTelemetryHealthCheckPublisher();

        // Configure Redis
        builder.Services.AddRedisServices(builder.Configuration);

        // Configure RabbitMQ
        builder.Services.AddRabbitMQServices(builder.Configuration);

        builder.Services.AddTransient<IDateTimeService, DateTimeService>();

        builder.Services.AddScoped<ICorrelationContextProvider, CorrelationContextProvider>();
        builder.Services.AddSingleton<ICorrelationContextSerializer, CorrelationContextSerializer>();

        return builder;
    }

    public static IServiceCollection AddFileServices(this IServiceCollection services)
    {
        services.AddSingleton<BindingWrapper>();
        services.AddSingleton<IConverter, HtmlConverter>();
        services.AddTransient<IFileService, FileService>();

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
}