using BiUM.Core.Authorization;
using BiUM.Core.Constants;
using BiUM.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Middlewares;

internal sealed class CorrelationContextExtractorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICorrelationContextSerializer _correlationContextSerializer;
    private readonly ILogger<CorrelationContextExtractorMiddleware> _logger;

    public CorrelationContextExtractorMiddleware(RequestDelegate next,
        ICorrelationContextSerializer correlationContextSerializer,
        ILogger<CorrelationContextExtractorMiddleware> logger)
    {
        _next = next;
        _correlationContextSerializer = correlationContextSerializer;
        _logger = logger;
    }

    public Task InvokeAsync(HttpContext context, ICorrelationContextAccessor correlationContextAccessor)
    {
        var headerValue = context.Request.Headers[HeaderKeys.CorrelationContext].ToString();

        if (string.IsNullOrEmpty(headerValue))
        {
            return _next.Invoke(context);
        }

        try
        {
            var bytes = Convert.FromBase64String(headerValue);

            var correlationContext = _correlationContextSerializer.Deserialize(bytes);

            if (correlationContext is not null)
            {
                correlationContextAccessor.CorrelationContext = correlationContext;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize CorrelationContext from header");
        }

        return _next.Invoke(context);
    }
}
