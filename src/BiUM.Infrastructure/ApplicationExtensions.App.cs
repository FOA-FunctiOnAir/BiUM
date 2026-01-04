using BiUM.Core.Common.Configs;
using BiUM.Core.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Middlewares;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    private const string UnhandledExceptionOccurred = "An unhandled exception occurred";
    private const string UnhandledExceptionOccurredWithNoException = $"{UnhandledExceptionOccurred}, no exception was provided by IExceptionHandlerFeature";

    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        var appOptionsAccessor = app.Services.GetService<IOptions<BiAppOptions>>();

        var appOptions = appOptionsAccessor?.Value;

        if (app.Environment.IsDevelopment() ||
            appOptions is not { Environment: "Production" or "Sandbox" })
        {
            app.UseSwagger();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwaggerUI(options =>
                {
                    options.DocumentTitle = $"BiApp.{appOptions?.Domain ?? "Unknown"}";
                });
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
                    var problemDetails = new ProblemDetails
                    {
                        Type = "unknown",
                        Title = UnhandledExceptionOccurred,
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = UnhandledExceptionOccurredWithNoException
                    };

                    await Results.Problem(problemDetails).ExecuteAsync(context);

                    logger.LogError(UnhandledExceptionOccurredWithNoException);

                    return;
                }

                var exception = exceptionHandlerFeature.Error;

                if (app.Environment.IsDevelopment() ||
                    appOptions is not { Environment: "Production" or "Sandbox" })
                {
                    var problemDetails = new ProblemDetails
                    {
                        Type = exception.ToProblemType(),
                        Title = exception.Message,
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = exception.ToString()
                    };

                    await Results.Problem(problemDetails).ExecuteAsync(context);
                }
                else
                {
                    var problemDetails = new ProblemDetails
                    {
                        Type = exception.ToProblemType(),
                        Title = UnhandledExceptionOccurred,
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = exception.Message
                    };

                    await Results.Problem(problemDetails).ExecuteAsync(context);
                }

                logger.LogError(exception, UnhandledExceptionOccurred);
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

        app.UseMiddleware<CorrelationContextActivityMiddleware>();
        app.UseMiddleware<ServiceCallMetricsMiddleware>();

        app.UseRouting();

        // Health API Endpoints
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
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

    private static string ToProblemType(this Exception exception)
    {
        var type = exception.GetType().Name;

        if (type.EndsWith(nameof(Exception), StringComparison.Ordinal))
        {
            type = type.Remove(type.Length - nameof(Exception).Length);
        }

        return type.ToSnakeCase();
    }

    private sealed class Application;

    private sealed class VersionResult
    {
        public required string Version { get; init; }
    }
}
