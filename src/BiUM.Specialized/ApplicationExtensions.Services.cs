using AutoMapper.Internal;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Infrastructure.Services.HttpClients;
using BiUM.Specialized.Interceptors;
using BiUM.Specialized.Services;
using BiUM.Specialized.Services.Crud;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OpenTelemetry.Trace;
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
            .AddApplicationPart(typeof(BiUM.Specialized.Common.API.ApiControllerBase).Assembly)
            .AddControllersAsServices()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddNpgsql()
                .AddSqlClientInstrumentation());

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
        services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));

        return services;
    }
}
