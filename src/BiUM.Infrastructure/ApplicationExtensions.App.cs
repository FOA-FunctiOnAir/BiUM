using BiUM.Core.Common.Configs;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    public static IApplicationBuilder UsesInfrastructure(this WebApplication app)
    {
        var appOptionsAccessor = app.Services.GetService<IOptions<BiAppOptions>>();

        var appOptions = appOptionsAccessor?.Value;

        if (app.Environment.IsDevelopment())
        {
            if (appOptions is not null)
            {
                app.UseSwagger();
            }
        }

        var logger = app.Services.GetRequiredService<ILogger<Application>>();

        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            AllowStatusCode404Response = false,
            CreateScopeForErrors = false,
            ExceptionHandler = async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();

                if (exceptionHandlerFeature?.Error is null)
                {
                    return;
                }

                context.Response.StatusCode = 500;

                await context.Response.WriteAsync("An unexpected error occurred");

                var exception = exceptionHandlerFeature.Error;

                logger.LogError(exception, "An unhandled exception occurred");
            }
        });

        AppDomain.CurrentDomain.UnhandledException +=
            (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    logger.LogError(ex, "An unhandled exception occurred");
                }
                else
                {
                    logger.LogError("An unhandled exception occurred, but no Exception object was provided");
                }
            };

        TaskScheduler.UnobservedTaskException +=
            (_, args) =>
            {
                logger.LogError(args.Exception, "An unobserved task exception occurred");

                args.SetObserved();
            };

        app.UseRouting();

        // Health API Endpoints
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapGet("/version", () => Results.Ok(new VersionResult
        {
            Version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "unknown"
        }));

        return app;
    }

    private sealed class Application;

    private sealed class VersionResult
    {
        public required string Version { get; init; }
    }
}