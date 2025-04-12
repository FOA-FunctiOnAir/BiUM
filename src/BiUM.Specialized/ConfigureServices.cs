using AutoMapper.Internal;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Infrastructure.Common.Interceptors;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Specialized.Services.Authorization;
using BiUM.Specialized.Services.HttpClients;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
                    .WithOrigins(
                        "http://localhost:3000",
                        "http://dev.bidyno.com",
                        "https://dev.bidyno.com",
                        "http://app.bidyno.com",
                        "https://app.bidyno.com"
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

        services.AddTransient<IDateTimeService, DateTimeService>();
        services.AddTransient<ICurrentUserService, CurrentUserService>();
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
}