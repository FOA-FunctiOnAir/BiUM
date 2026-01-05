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
        _ = builder.Services.AddControllersWithViews();

        _ = builder.Services.AddRazorPages();

        _ = builder.Services.AddControllers()
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
        _ = builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

        // Configure Grpc
        _ = builder.Services.AddGrpc();
        _ = builder.Services.AddGrpcReflection();

        _ = builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddGrpcCoreInstrumentation()
                .AddGrpcClientInstrumentation(options =>
                {
                    options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                    {
                        _ = activity.SetTag("grpc.request.uri", httpRequestMessage.RequestUri);
                    };
                    options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                    {
                        _ = activity.SetTag("grpc.response.status_code", (int)httpResponseMessage.StatusCode);
                    };
                })
                .AddNpgsql()
                .AddSqlClientInstrumentation());

        _ = builder.Services.Configure<BiGrpcOptions>(builder.Configuration.GetSection(BiGrpcOptions.Name));
        _ = builder.Services.Configure<BiMailOptions>(builder.Configuration.GetSection(BiMailOptions.Name));

        _ = builder.Services.AddScoped<EntitySaveChangesInterceptor>();

        _ = builder.Services.AddTransient<ICrudService, CrudService>();
        _ = builder.Services.AddTransient<IHttpClientsService, HttpClientService>();
        _ = builder.Services.AddTransient<ITranslationService, TranslationService>();

        return builder;
    }

    public static IServiceCollection AddInfrastructureAdditionalServices<TAssembly>(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(TAssembly).Assembly;

        _ = services.AddAutoMapper(cfg => cfg.Internal().MethodMappingEnabled = false, assembly);
        _ = services.AddValidatorsFromAssembly(assembly);
        _ = services.AddMediatR(assembly);

        return services;
    }

    public static IServiceCollection AddGrpcClient<TClient>(this IServiceCollection services, IConfiguration configuration, string serviceKey)
        where TClient : ClientBase<TClient>
    {
        var grpcOptions = configuration.GetSection(BiGrpcOptions.Name).Get<BiGrpcOptions>()
            ?? throw new InvalidOperationException($"{BiGrpcOptions.Name} not found in configuration");

        var url = grpcOptions.GetServiceUrl(serviceKey);

        _ = services.AddScoped<ForwardHeadersGrpcInterceptor>();

        _ = services.AddGrpcClient<TClient>(o => o.Address = new Uri(url))
            .AddInterceptor<ForwardHeadersGrpcInterceptor>();

        return services;
    }
}
