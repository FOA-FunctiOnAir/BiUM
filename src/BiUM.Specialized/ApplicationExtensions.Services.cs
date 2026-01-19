using AutoMapper.Internal;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Interceptors;
using BiUM.Specialized.MagicOnion;
using BiUM.Specialized.Services;
using BiUM.Specialized.Services.Crud;
using BiUM.Specialized.Services.HttpClients;
using FluentValidation;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Serialization;
using MediatR;
using MemoryPack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Trace;
using System;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    public static WebApplicationBuilder ConfigureSpecializedServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();

        builder.Services.AddRazorPages();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(BiUM.Specialized.Common.API.CrudController).Assembly)
            .AddApplicationPart(typeof(BiUM.Specialized.Common.API.DomainCrudController).Assembly)
            .AddApplicationPart(typeof(BiUM.Specialized.Common.API.DomainTranslationController).Assembly)
            .AddControllersAsServices()
            .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

        // Configure MagicOnion Rpc
        var magicOnionSerializerProvider = MemoryPackWithBrotliMagicOnionSerializerProvider.Create(MemoryPackSerializerOptions.Default, CompressionLevel.Optimal);

        MagicOnionSerializerProvider.Default = magicOnionSerializerProvider;

        builder.Services.AddSingleton<IMagicOnionSerializerProvider>(magicOnionSerializerProvider);

        builder.Services.AddMagicOnion(options =>
        {
            options.MessageSerializer = magicOnionSerializerProvider;
        });

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
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
                .AddNpgsql()
                .AddSqlClientInstrumentation());

        builder.Services.Configure<BiGrpcOptions>(builder.Configuration.GetSection(BiGrpcOptions.Name));
        builder.Services.Configure<BiMailOptions>(builder.Configuration.GetSection(BiMailOptions.Name));

        builder.Services.AddScoped<EntitySaveChangesInterceptor>();

        builder.Services.AddTransient<ICrudService, CrudService>();
        builder.Services.AddTransient<IHttpClientsService, HttpClientService>();
        builder.Services.AddTransient<ITranslationService, TranslationService>();

        return builder;
    }

    public static IServiceCollection AddInfrastructureAdditionalServices<TMarker>(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(TMarker).Assembly;

        services.AddAutoMapper(cfg => cfg.Internal().MethodMappingEnabled = false, assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddMediatR(assembly);

        return services;
    }

    public static IServiceCollection AddRpcClient<TClient, TClientImpl>(this IServiceCollection services, string serviceKey)
        where TClient : class, IService<TClient>
        where TClientImpl : class, IMagicOnionRpcClient<TClient>
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

        services.TryAddScoped<CorrelationContextMagicOnionClientFilter>();
        services.TryAddScoped<ForwardHeadersMagicOnionClientFilter>();

        TClientImpl.TryRegisterProviderFactory();
        TClientImpl.RegisterMemoryPackFormatters();

        services.AddScoped<TClient>(sp =>
        {
            var channel = sp.GetRequiredKeyedService<GrpcChannel>(serviceKey);
            var correlationContextFilter = sp.GetRequiredService<CorrelationContextMagicOnionClientFilter>();
            var forwardHeadersFilter = sp.GetRequiredService<ForwardHeadersMagicOnionClientFilter>();
            var serializerProvider = sp.GetRequiredService<IMagicOnionSerializerProvider>();

            var client = MagicOnionClient.Create<TClient>(
                channel,
                clientFactoryProvider: TClientImpl.ClientFactoryProvider,
                serializerProvider: serializerProvider,
                clientFilters: [forwardHeadersFilter, correlationContextFilter]);

            return client;
        });

        return services;
    }
}
