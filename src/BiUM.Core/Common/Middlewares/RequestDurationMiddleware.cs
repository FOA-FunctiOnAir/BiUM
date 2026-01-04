using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BiUM.Core.Common.Middlewares;

public sealed class RequestDurationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestDurationMiddleware> _logger;

    public RequestDurationMiddleware(RequestDelegate next, ILogger<RequestDurationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            await _next.Invoke(context);
            return;
        }

        var startTs = Stopwatch.GetTimestamp();

        // execute the request pipeline
        await _next.Invoke(context);

        var elapsedTime = Stopwatch.GetElapsedTime(startTs);

        _logger.LogDebug("Request {RequestId} took {ElapsedTime}ms", context.TraceIdentifier, elapsedTime);
    }
}
