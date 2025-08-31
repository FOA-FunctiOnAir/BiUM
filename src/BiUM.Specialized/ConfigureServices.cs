using AutoMapper.Internal;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Infrastructure.Common.Interceptors;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Infrastructure.Services.File;
using BiUM.Specialized.Services.Authorization;
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

        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services.Configure<BiAppOptions>(configuration.GetSection(BiAppOptions.Name));
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

        services.AddTransient<ICurrentUserService, CurrentUserService>();
        services.AddTransient<IDateTimeService, DateTimeService>();
        services.AddTransient<IHttpClientsService, HttpClientService>();

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
}