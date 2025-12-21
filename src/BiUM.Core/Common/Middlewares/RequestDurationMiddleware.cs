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
        var startTs = Stopwatch.GetTimestamp();

        // execute the request pipeline
        await _next.Invoke(context);

        var elapsedTime = Stopwatch.GetElapsedTime(startTs);

        _logger.LogInformation("Request {RequestId} took {ElapsedTime}ms", context.TraceIdentifier, elapsedTime);
    }
}