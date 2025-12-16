using AutoMapper.Internal;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Infrastructure.Services.File;
using BiUM.Infrastructure.Services.Serialization;
using BiUM.Specialized.Interceptors;
using BiUM.Specialized.Services;
using BiUM.Specialized.Services.Authorization;
using BiUM.Specialized.Services.Crud;
using BiUM.Specialized.Services.File;
using BiUM.Specialized.Services.HttpClients;
using BiUM.Specialized.Services.Serialization;
using FluentValidation;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using SimpleHtmlToPdf;
using SimpleHtmlToPdf.Interfaces;
using SimpleHtmlToPdf.UnmanagedHandler;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddSpecializedServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddControllersWithViews();

        services.AddRazorPages();

        services.AddCors(options =>
        {
            options.AddPolicy(
                name: BiUM.Specialized.Consts.Application.BiAppOrigins,
                policy =>
                {
                    policy
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .WithOrigins(
                        "http://localhost:3000",
                        "http://*.bidyno.com",
                        "https://*.bidyno.com"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });
        });

        services.AddControllers()
            .AddApplicationPart(typeof(BiUM.Specialized.Common.API.CrudController).Assembly)
            .AddApplicationPart(typeof(BiUM.Specialized.Common.API.DomainCrudController).Assembly)
            .AddApplicationPart(typeof(BiUM.Specialized.Common.API.DomainTranslationController).Assembly)
            .AddControllersAsServices()
            .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        var appOptionsSection = configuration.GetSection(BiAppOptions.Name);

        services.Configure<BiAppOptions>(appOptionsSection);
        services.Configure<BiGrpcOptions>(configuration.GetSection(BiGrpcOptions.Name));
        services.Configure<BiMailOptions>(configuration.GetSection(BiMailOptions.Name));

        // Customise default API behaviour
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
        services.Configure<HttpClientsOptions>(configuration.GetSection(HttpClientsOptions.Name));

        services.AddEndpointsApiExplorer();

        var appOptions = appOptionsSection.Get<BiAppOptions>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddSwaggerGen(c =>
        {
            if (appOptions is not null)
            {
                c.SwaggerDoc(appOptions.DomainVersion, new OpenApiInfo { Title = $"BiApp {appOptions.Domain} APIs", Version = appOptions.DomainVersion });
            }
        });

        services.AddScoped<EntitySaveChangesInterceptor>();

        services.AddScoped<ICorrelationContextProvider, CorrelationContextProvider>();
        services.AddSingleton<ICorrelationContextSerializer, CorrelationContextSerializer>();

        services.AddTransient<ICrudService, CrudService>();
        services.AddTransient<IDateTimeService, DateTimeService>();
        services.AddTransient<IHttpClientsService, HttpClientService>();
        services.AddTransient<ITranslationService, TranslationService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureAdditionalServices(this IServiceCollection services, IConfiguration configuration, Assembly assembly)
    {
        services.AddAutoMapper(cfg => cfg.Internal().MethodMappingEnabled = false, assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddMediatR(assembly);

        return services;
    }

    public static IServiceCollection AddFileServices(this IServiceCollection services)
    {
        services.AddSingleton<BindingWrapper>();
        services.AddSingleton<IConverter, HtmlConverter>();
        services.AddTransient<IFileService, FileService>();

        return services;
    }

    public static IServiceCollection AddGrpcClient<TClient>(this IServiceCollection services, IConfiguration configuration, string serviceKey)
        where TClient : ClientBase<TClient>
    {
        var grpcOptions = configuration.GetSection(BiGrpcOptions.Name).Get<BiGrpcOptions>();

        if (grpcOptions is null)
        {
            throw new InvalidOperationException($"{BiGrpcOptions.Name} not found in configuration");
        }

        var url = grpcOptions.GetServiceUrl(serviceKey);

        services.AddScoped<ForwardHeadersGrpcInterceptor>();

        services.AddGrpcClient<TClient>(o => o.Address = new Uri(url))
            .AddInterceptor<ForwardHeadersGrpcInterceptor>();

        return services;
    }
}