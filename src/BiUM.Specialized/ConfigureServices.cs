using AutoMapper.Internal;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Infrastructure.Services.File;
using BiUM.Specialized.Interceptors;
using BiUM.Specialized.Services;
using BiUM.Specialized.Services.Authorization;
using BiUM.Specialized.Services.Crud;
using BiUM.Specialized.Services.File;
using BiUM.Specialized.Services.HttpClients;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SimpleHtmlToPdf;
using SimpleHtmlToPdf.Interfaces;
using SimpleHtmlToPdf.UnmanagedHandler;
using System.Reflection;
using System.Text;
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
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
//            options.JsonSerializerOptions.Converters.Insert(0, new JsonBoolConverter());
//            options.JsonSerializerOptions.Converters.Insert(1, new JsonIntConverter());
//            options.JsonSerializerOptions.Converters.Insert(2, new JsonLongConverter());
//            options.JsonSerializerOptions.Converters.Insert(3, new JsonShortConverter());
//            options.JsonSerializerOptions.Converters.Insert(4, new JsonByteConverter());
//            options.JsonSerializerOptions.Converters.Insert(5, new JsonSByteConverter());
//            options.JsonSerializerOptions.Converters.Insert(6, new JsonUIntConverter());
//            options.JsonSerializerOptions.Converters.Insert(7, new JsonULongConverter());
//            options.JsonSerializerOptions.Converters.Insert(8, new JsonUShortConverter());
//            options.JsonSerializerOptions.Converters.Insert(9, new JsonFloatConverter());
//            options.JsonSerializerOptions.Converters.Insert(10, new JsonDoubleConverter());
//            options.JsonSerializerOptions.Converters.Insert(11, new JsonDecimalConverter());
//            options.JsonSerializerOptions.Converters.Insert(12, new JsonDateTimeLenientConverter());
//            options.JsonSerializerOptions.Converters.Insert(13, new JsonDateTimeOffsetLenientConverter());
//#if NET6_0_OR_GREATER
//            options.JsonSerializerOptions.Converters.Add(new JsonDateOnlyLenientConverter());
//            options.JsonSerializerOptions.Converters.Add(new JsonTimeOnlyLenientConverter());
//#endif
//            options.JsonSerializerOptions.Converters.Add(new JsonGuidConverter());
//            options.JsonSerializerOptions.Converters.Add(new JsonEnumNullConverterFactory());
//            options.JsonSerializerOptions.Converters.Add(new JsonTimeSpanConverter());
//            options.JsonSerializerOptions.Converters.Add(new JsonNullToEmptyListConverterFactory());
//            options.JsonSerializerOptions.Converters.Add(new JsonNullableBoolConverter());

            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        services.Configure<BiAppOptions>(configuration.GetSection(BiAppOptions.Name));
        services.Configure<BiGrpcOptions>(configuration.GetSection(BiGrpcOptions.Name));
        services.Configure<BiMailOptions>(configuration.GetSection(BiMailOptions.Name));

        // Customise default API behaviour
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
        services.Configure<HttpClientsOptions>(configuration.GetSection(HttpClientsOptions.Name));

        services.AddEndpointsApiExplorer();

        var serviceProvider = services.BuildServiceProvider();
        var appOptions = serviceProvider.GetRequiredService<IOptions<BiAppOptions>>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(appOptions.Value.DomainVersion, new OpenApiInfo { Title = $"BiApp {appOptions.Value.Domain} APIs", Version = appOptions.Value.DomainVersion });
        });

        services.AddScoped<EntitySaveChangesInterceptor>();

        services.AddTransient<ICorrelationContextProvider, CorrelationContextProvider>();

        services.AddTransient<ICrudService, CrudService>();
        services.AddTransient<ICurrentUserService, CurrentUserService>();
        services.AddTransient<IDateTimeService, DateTimeService>();
        services.AddTransient<IHttpClientsService, HttpClientService>();
        services.AddTransient<ITranslationService, TranslationService>();

        services.AddAuthentication();

        services.AddAuthorizationBuilder()
            .AddPolicy("CanPurge", policy => policy.RequireRole("Administrator"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my secret key is galatasaray because it can happy to me all time"));

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;

            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,

                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
            };
        });

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

    public static IServiceCollection AddGrpcClients<TClient>(this IServiceCollection services, string microserviceName)
        where TClient : class
    {
        var serviceProvider = services.BuildServiceProvider();
        var grpcOptions = serviceProvider.GetRequiredService<IOptions<BiGrpcOptions>>();

        var url = grpcOptions.Value.GetDomain(microserviceName);

        services.AddTransient<ForwardHeadersGrpcInterceptor>();

        services.AddGrpcClient<TClient>(o =>
        {
            o.Address = new Uri(url);
        }).AddInterceptor<ForwardHeadersGrpcInterceptor>();

        return services;
    }
}