using BiUM.Core.Authorization;
using BiUM.Core.Caching.Redis;
using BiUM.Core.Common.Configs;
using BiUM.Core.File;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Core.Serialization;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.MagicOnion.Client;
using BiUM.Infrastructure.MagicOnion.Filters.Client;
using BiUM.Infrastructure.MagicOnion.Filters.Server;
using BiUM.Infrastructure.MagicOnion.Serialization;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Infrastructure.Services.Caching.Redis;
using BiUM.Infrastructure.Services.File;
using BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;
using BiUM.Specialized.Services.Serialization;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Serialization;
using MagicOnion.Server;
using MemoryPack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using SimpleHtmlToPdf;
using SimpleHtmlToPdf.Interfaces;
using SimpleHtmlToPdf.UnmanagedHandler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    private static readonly TimeSpan FastRedisDurationLimit = TimeSpan.FromMilliseconds(100);

    public static WebApplicationBuilder ConfigureInfrastructureServices(this WebApplicationBuilder builder)
    {
        var appOptionsSection = builder.Configuration.GetSection(BiAppOptions.Name);

        builder.Services.Configure<BiAppOptions>(appOptionsSection);

        var appOptions = appOptionsSection.Get<BiAppOptions>();

        var isNotProductionLike =
            builder.Environment.IsDevelopment() ||
            appOptions is not { Environment: "Production" or "Sandbox" or "Staging" or "QA" };

        builder.Services.Configure<HttpClientsOptions>(builder.Configuration.GetSection(HttpClientsOptions.Name));
        builder.Services.Configure<BiGrpcOptions>(builder.Configuration.GetSection(BiGrpcOptions.Name));

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddHttpClient();

        builder.Services.AddHealthChecks();

        var serviceName = $"BiApp.{appOptions?.Domain ?? "Unknown"}";

        builder.Services.AddSingleton(_ => new ActivitySource(serviceName));

        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
            logging.AddOtlpExporter();
            logging.AddConsoleExporter(builder.Environment);
        });

        builder.Services.AddOpenTelemetry()
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
                            activity.SetTag("error.stack_trace", exception.StackTrace);
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            activity.SetTag("db.name", command.Connection?.Database);
                        };
                    })
                    .AddGrpcCoreInstrumentation()
                    .AddGrpcClientInstrumentation(options =>
                    {
                        options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                        {
                            activity.SetTag("grpc.request.uri", httpRequestMessage.RequestUri);
                        };
                        options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                        {
                            activity.SetTag("grpc.response.status_code", (int)httpResponseMessage.StatusCode);
                        };
                    })
                    .AddRabbitMQInstrumentation()
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

        if (isNotProductionLike)
        {
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddSwaggerGen(options =>
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
        builder.Services.AddHealthChecks()
            .AddApplicationLifecycleHealthCheck()
            .AddManualHealthCheck()
            // .AddResourceUtilizationHealthCheck(o =>
            // {
            //     o.CpuThresholds = new ResourceUsageThresholds
            //     {
            //         DegradedUtilizationPercentage = 90,
            //         UnhealthyUtilizationPercentage = 95
            //     };
            //     o.MemoryThresholds = new ResourceUsageThresholds
            //     {
            //         DegradedUtilizationPercentage = 90,
            //         UnhealthyUtilizationPercentage = 95
            //     };
            // })
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

        builder.Services.AddTelemetryHealthCheckPublisher();

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

        // Configure MagicOnion Rpc
        var magicOnionSerializerProvider = MemoryPackWithBrotliSerializerProvider.Create(MemoryPackSerializerOptions.Default, CompressionLevel.Optimal);

        MagicOnionSerializerProvider.Default = magicOnionSerializerProvider;

        builder.Services.AddSingleton<IMagicOnionSerializerProvider>(magicOnionSerializerProvider);

        builder.Services.AddSingleton<GlobalApiResponseFilter>();

        builder.Services.AddMagicOnion(options =>
        {
            options.MessageSerializer = magicOnionSerializerProvider;
            options.IsReturnExceptionStackTraceInErrorDetail = isNotProductionLike;
            options.EnableCurrentContext = true;

            options.GlobalFilters.Add<GlobalApiResponseFilter>();
        });

        // Configure Redis
        builder.Services.AddRedisServices(builder.Configuration);

        // Configure RabbitMQ
        builder.Services.AddRabbitMqServices(builder.Configuration);

        builder.Services.AddTransient<IDateTimeService, DateTimeService>();

        builder.Services.AddScoped<ICorrelationContextProvider, CorrelationContextProvider>();
        builder.Services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
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

    public static IServiceCollection AddRpcClient<TClient, TProviderWrapper>(this IServiceCollection services, string serviceKey)
        where TClient : class, IService<TClient>
        where TProviderWrapper : class, IClientFactoryProviderWrapper<TClient>
    {
        services.TryAddKeyedSingleton(
            serviceKey,
            (sp, objectKey) =>
            {
                ArgumentNullException.ThrowIfNull(objectKey);

                if (objectKey is not string key)
                {
                    throw  new InvalidOperationException($"Expected string key but provided {objectKey.GetType().Name}");
                }

                var grpcOptionsAccessor = sp.GetRequiredService<IOptions<BiGrpcOptions>>();

                var grpcOptions = grpcOptionsAccessor.Value;

                var url = grpcOptions.GetServiceUrl(key);

                return GrpcChannel.ForAddress(url);
            });

        services.TryAddScoped<CorrelationContextFilter>();
        services.TryAddScoped<ForwardHeadersFilter>();

        TProviderWrapper.TryRegisterProviderFactory();
        TProviderWrapper.RegisterMemoryPackFormatters();

        services.AddScoped<TClient>(sp =>
        {
            var channel = sp.GetRequiredKeyedService<GrpcChannel>(serviceKey);
            var correlationContextFilter = sp.GetRequiredService<CorrelationContextFilter>();
            var forwardHeadersFilter = sp.GetRequiredService<ForwardHeadersFilter>();
            var serializerProvider = sp.GetRequiredService<IMagicOnionSerializerProvider>();

            var client = MagicOnionClient.Create<TClient>(
                channel,
                clientFactoryProvider: TProviderWrapper.ClientFactoryProvider,
                serializerProvider: serializerProvider,
                clientFilters: [forwardHeadersFilter, correlationContextFilter]);

            return client;
        });

        return services;
    }

    private static IServiceCollection AddRabbitMqServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMQOptions>(configuration.GetSection(RabbitMQOptions.Name));

        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMQOptions>>().Value;

            var factory = new ConnectionFactory
            {
                Uri = new Uri($"amqp://{options.UserName}:{options.Password}@{options.Hostname}:{options.Port}/{options.VirtualHost}"),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(options.NetworkRecoveryIntervalSeconds),
                TopologyRecoveryEnabled = true
            };

            return factory;
        });

        services.AddSingleton<RabbitMQConnectionProvider>();
        services.AddSingleton<RabbitMQPublisherChannelPool>();

        services.AddSingleton<IRabbitMQSerializer, RabbitMQSerializer>();
        services.AddSingleton<IRabbitMQClient, RabbitMQClient>();

        services.AddHostedService<RabbitMQListenerService>();

        services.AddRabbitMQEventHandlers();

        // Add RabbitMQ Health Check
        services.AddHealthChecks()
            .AddCheck<RabbitMQHealthCheck>("rabbitmq", tags: ["ready"]);

        return services;
    }

    private static IServiceCollection AddRedisServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisClientOptions>(configuration.GetSection(RedisClientOptions.Name));

        services.AddSingleton<IRedisClient, RedisClient>();

        return services;
    }
}
