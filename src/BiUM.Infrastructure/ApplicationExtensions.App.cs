using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.Middlewares;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
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

        var isNotProductionLike =
            app.Environment.IsDevelopment() ||
            appOptions is not { Environment: "Production" or "Sandbox" or "Staging" or "QA" };

        if (isNotProductionLike)
        {
            _ = app.UseSwagger();

            if (app.Environment.IsDevelopment())
            {
                _ = app.UseSwaggerUI(options =>
                {
                    options.DocumentTitle = $"BiApp.{appOptions?.Domain ?? "Unknown"}";
                });
            }
        }

        var logger = app.Services.GetRequiredService<ILogger<Application>>();

        // 1. Exception Handler for MagicOnion/gRPC requests
        _ = app.UseWhen(context => context.Request.ContentType == "application/grpc", grpcApp =>
            grpcApp.UseMiddleware<GrpcGlobalExceptionHandlerMiddleware>());

        // 2. Exception Handler REST/JSON requests
        _ = app.UseWhen(context => context.Request.ContentType != "application/grpc", restApp =>
            restApp.UseExceptionHandler(new ExceptionHandlerOptions
            {
                AllowStatusCode404Response = false,
                CreateScopeForErrors = false,
                ExceptionHandler = async context =>
                {
                    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();

                    var jsonSerializerOptions = context.RequestServices.GetService<JsonSerializerOptions>();

                    var response = new ApiResponse();

                    context.Response.ContentType = "application/problem+json";

                    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    context.Response.Headers["Expires"] = "0";
                    context.Response.Headers["Pragma"] = "no-cache";

                    if (exceptionHandlerFeature?.Error is null)
                    {
                        response.AddMessage(new ResponseMessage()
                        {
                            Code = "unknown_error",
                            Message = UnhandledExceptionOccurred,
                            Exception = UnhandledExceptionOccurredWithNoException,
                            Severity = MessageSeverity.Error
                        });

                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                        await context.Response.WriteAsJsonAsync(response, jsonSerializerOptions, context.RequestAborted);

                        logger.LogError(UnhandledExceptionOccurredWithNoException);

                        return;
                    }

                    var exception = exceptionHandlerFeature.Error;

                    if (isNotProductionLike)
                    {
                        response.AddMessage(new ResponseMessage()
                        {
                            Code = exception.ToErrorCode(),
                            Message = exception.Message,
                            Exception = exception.ToString(),
                            Severity = MessageSeverity.Error
                        });
                    }
                    else
                    {
                        response.AddMessage(new ResponseMessage()
                        {
                            Code = exception.ToErrorCode(),
                            Message = UnhandledExceptionOccurred,
                            Exception = exception.Message,
                            Severity = MessageSeverity.Error
                        });
                    }

                    context.Response.StatusCode = exception.ToStatusCode();

                    await context.Response.WriteAsJsonAsync(response, jsonSerializerOptions, context.RequestAborted);

                    logger.LogError(exception, UnhandledExceptionOccurred);
                }
            }));

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

        _ = app.UseMiddleware<CorrelationContextExtractorMiddleware>();
        _ = app.UseMiddleware<CorrelationContextActivityMiddleware>();
        _ = app.UseMiddleware<ServiceCallMetricsMiddleware>();

        _ = app.UseRouting();

        // Health API Endpoints
        _ = app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        _ = app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        _ = app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        _ = app.MapGet("/version", () => Results.Ok(new VersionResult
        {
            Version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "unknown"
        }));

        _ = app.MapMagicOnionService();

        return app;
    }

    private sealed class Application;

    private sealed class VersionResult
    {
        public required string Version { get; init; }
    }
}