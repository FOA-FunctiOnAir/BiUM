using AutoMapper.Internal;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Interceptors;
using BiUM.Specialized.Services;
using BiUM.Specialized.Services.Crud;
using BiUM.Specialized.Services.HttpClients;
using FluentValidation;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OpenTelemetry.Trace;
using System;
using System.Reflection;
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

        // Configure Grpc
        builder.Services.AddGrpc();
        builder.Services.AddGrpcReflection();

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

    public static IServiceCollection AddGrpcClient<TClient>(this IServiceCollection services, IConfiguration configuration, string serviceKey)
        where TClient : ClientBase<TClient>
    {
        var grpcOptions = configuration.GetSection(BiGrpcOptions.Name).Get<BiGrpcOptions>()
            ?? throw new InvalidOperationException($"{BiGrpcOptions.Name} not found in configuration");

        var url = grpcOptions.GetServiceUrl(serviceKey);

        services.AddScoped<ForwardHeadersGrpcInterceptor>();

        services.AddGrpcClient<TClient>(o => o.Address = new Uri(url))
            .AddInterceptor<ForwardHeadersGrpcInterceptor>();

        return services;
    }
}
