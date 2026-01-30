using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.MagicOnion;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Middlewares;

internal sealed class GrpcGlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GrpcGlobalExceptionHandlerMiddleware> _logger;

    private readonly bool _isNotProductionLike;

    public GrpcGlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        IHostEnvironment environment,
        IOptions<BiAppOptions> appOptionsAccessor,
        ILogger<GrpcGlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        _isNotProductionLike =
            environment.IsDevelopment() ||
            appOptionsAccessor.Value is not { Environment: "Production" or "Sandbox" or "Staging" or "QA" };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing gRPC request");

            if (context.Response.HasStarted)
            {
                return;
            }

            var statusCode = ex.ToGrpcStatusCode();

            // gRPC errors are conveyed via response Trailers, not in the response body.
            context.Response.StatusCode = 200; // gRPC always returns 200 OK for application-level errors
            context.Response.ContentType = "application/grpc";

            // Set gRPC status code and message in trailers
            context.Response.AppendTrailer("grpc-status", ((int)statusCode).ToString());
            context.Response.AppendTrailer("grpc-message", _isNotProductionLike ? ex.ToString() : ex.Message);

            await context.Response.WriteAsync(string.Empty);
        }
    }


}