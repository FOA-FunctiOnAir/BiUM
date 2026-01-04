using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection.Middlewares;

public class ServiceCallMetricsMiddleware
{
    private readonly BiAppOptions? _biAppOptions;
    private readonly RequestDelegate _next;
    private readonly IRabbitMQClient? _rabbitMQClient;
    private readonly ILogger<ServiceCallMetricsMiddleware> _logger;

    public ServiceCallMetricsMiddleware(
        RequestDelegate next,
        IRabbitMQClient? rabbitMQClient,
        IOptions<BiAppOptions>? biAppOptions,
        ILogger<ServiceCallMetricsMiddleware> logger)
    {
        _biAppOptions = biAppOptions?.Value;
        _next = next;
        _rabbitMQClient = rabbitMQClient;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_rabbitMQClient is null || _biAppOptions is null || ShouldIgnoreRequest(context))
        {
            await _next.Invoke(context);

            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var isSuccess = false;

        try
        {
            await _next(context);

            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            isSuccess = statusCode is >= 200 and < 300;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            isSuccess = false;

            throw;
        }
        finally
        {
            try
            {
                var serviceCalledEvent = new ServiceCalledEvent
                {
                    ServiceName = FormatServiceName(_biAppOptions.Domain),
                    Endpoint = context.Request.Path + context.Request.QueryString,
                    HttpMethod = context.Request.Method,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    Success = isSuccess
                };

                await _rabbitMQClient.PublishAsync(serviceCalledEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish ServiceCalledEvent for {Path}", context.Request.Path);
            }
        }
    }

    private static bool ShouldIgnoreRequest(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        return path.StartsWith("/health") ||
               path.StartsWith("/swagger") ||
               path.StartsWith("/version") ||
               path.StartsWith("/favicon.ico");
    }

    private static string FormatServiceName(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            return "Unknown";
        }

        return serviceName.StartsWith("BiApp.", StringComparison.OrdinalIgnoreCase) ? serviceName : $"BiApp.{serviceName}";
    }
}
