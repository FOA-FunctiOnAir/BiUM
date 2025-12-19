using AutoMapper.Internal;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Infrastructure.Services.File;
using BiUM.Specialized.Interceptors;
using BiUM.Specialized.Services;
using BiUM.Specialized.Services.Crud;
using BiUM.Specialized.Services.File;
using BiUM.Specialized.Services.HttpClients;
using FluentValidation;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SimpleHtmlToPdf;
using SimpleHtmlToPdf.Interfaces;
using SimpleHtmlToPdf.UnmanagedHandler;
using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    public static IServiceCollection AddSpecializedServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithViews();

        services.AddRazorPages();

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

        // Customise default API behaviour
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

        // Configure Grpc
        services.AddGrpc();
        services.AddGrpcReflection();

        services.Configure<BiGrpcOptions>(configuration.GetSection(BiGrpcOptions.Name));
        services.Configure<BiMailOptions>(configuration.GetSection(BiMailOptions.Name));

        services.AddScoped<EntitySaveChangesInterceptor>();

        services.AddTransient<ICrudService, CrudService>();
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