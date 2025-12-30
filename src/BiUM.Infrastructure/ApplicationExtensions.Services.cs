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
using Microsoft.Extensions.Hosting;
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
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    private static readonly TimeSpan FastRedisDurationLimit = TimeSpan.FromMilliseconds(100);

    public static WebApplicationBuilder ConfigureInfrastructureServices(this WebApplicationBuilder builder)
    {
        var appOptionsSection = builder.Configuration.GetSection(BiAppOptions.Name);

        _ = builder.Services.Configure<BiAppOptions>(appOptionsSection);

        var appOptions = appOptionsSection.Get<BiAppOptions>();

        _ = builder.Services.Configure<HttpClientsOptions>(builder.Configuration.GetSection(HttpClientsOptions.Name));

        _ = builder.Services.AddHttpContextAccessor();

        _ = builder.Services.AddHttpClient();

        _ = builder.Services.AddHealthChecks();

        var serviceName = $"BiApp.{appOptions?.Domain ?? "Unknown"}";

        _ = builder.Logging.ClearProviders();
        _ = builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
            _ = logging.AddOtlpExporter();
            _ = logging.AddConsoleExporter(builder.Environment);
        });

        _ = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource
                    .AddService(serviceName)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = string.IsNullOrEmpty(appOptions?.Environment) ? builder.Environment.EnvironmentName : appOptions.Environment
                    }))
            .WithMetrics(metrics =>
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddOtlpExporter()
                    .AddConsoleExporter(builder.Configuration, builder.Environment))
            .WithTracing(tracing =>
                tracing
                    .AddSource(serviceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.EnrichWithException = (activity, exception) =>
                        {
                            _ = activity.SetTag("error.stack_trace", exception.StackTrace);
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            _ = activity.SetTag("db.name", command.Connection?.Database);
                        };
                    })
                    .AddRedisInstrumentation(options =>
                        options.Enrich = (activity, context) =>
                        {
                            _ = activity.SetTag("redis.duration", context.ProfiledCommand.ElapsedTime.TotalMilliseconds);

                            if (context.ProfiledCommand.ElapsedTime < FastRedisDurationLimit)
                            {
                                _ = activity.SetTag("redis.is_fast", true);
                            }
                        })
                    .AddOtlpExporter()
                    .AddConsoleExporter(builder.Configuration, builder.Environment));

        _ = builder.Services.AddEndpointsApiExplorer();

        if (builder.Environment.IsDevelopment() ||
            appOptions is not { Environment: "Production" or "Sandbox" })
        {
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            _ = builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(
                    appOptions?.DomainVersion ?? "v1",
                    new OpenApiInfo
                    {
                        Title = $"BiApp {appOptions?.Domain} APIs",
                        Version = appOptions?.DomainVersion ?? "v1"
                    });
            });
        }

        // Health Checks
        _ = builder.Services.AddHealthChecks()
            .AddApplicationLifecycleHealthCheck()
            .AddManualHealthCheck()
            .AddResourceUtilizationHealthCheck(o =>
            {
                o.CpuThresholds = new ResourceUsageThresholds
                {
                    DegradedUtilizationPercentage = 90,
                    UnhealthyUtilizationPercentage = 95
                };
                o.MemoryThresholds = new ResourceUsageThresholds
                {
                    DegradedUtilizationPercentage = 90,
                    UnhealthyUtilizationPercentage = 95
                };
            })
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

        _ = builder.Services.AddTelemetryHealthCheckPublisher();

        // Configure Redis
        _ = builder.Services.AddRedisServices(builder.Configuration);

        // Configure RabbitMQ
        _ = builder.Services.AddRabbitMqServices(builder.Configuration);

        _ = builder.Services.AddTransient<IDateTimeService, DateTimeService>();

        _ = builder.Services.AddScoped<ICorrelationContextProvider, CorrelationContextProvider>();
        _ = builder.Services.AddSingleton<ICorrelationContextSerializer, CorrelationContextSerializer>();

        return builder;
    }

    public static IServiceCollection AddFileServices(this IServiceCollection services)
    {
        _ = services.AddSingleton<BindingWrapper>();
        _ = services.AddSingleton<IConverter, HtmlConverter>();
        _ = services.AddTransient<IFileService, FileService>();

        return services;
    }

    private static IServiceCollection AddRabbitMqServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<RabbitMQOptions>(configuration.GetSection(RabbitMQOptions.Name));
        _ = services.AddSingleton<IRabbitMQClient, RabbitMQClient>();
        _ = services.AddHostedService<RabbitMQListenerService>();
        _ = services.AddRabbitMQEventHandlers();

        // Add RabbitMQ Health Check
        _ = services.AddHealthChecks()
            .AddCheck<RabbitMQHealthCheck>("rabbitmq", tags: ["ready"]);

        return services;
    }

    private static IServiceCollection AddRedisServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<RedisClientOptions>(configuration.GetSection(RedisClientOptions.Name));
        _ = services.AddSingleton<IRedisClient, RedisClient>();

        return services;
    }
}
