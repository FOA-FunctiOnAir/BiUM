using BiUM.Core.Authorization;
using BiUM.Core.Consts;
using BiUM.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection.Middlewares;

internal sealed class CorrelationContextExtractorMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationContextExtractorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(
        HttpContext context,
        ICorrelationContextAccessor correlationContextAccessor,
        ICorrelationContextSerializer correlationContextSerializer,
        ILogger<CorrelationContextExtractorMiddleware> logger)
    {
        var headerValue = context.Request.Headers[HeaderKeys.CorrelationContext].ToString();

        if (string.IsNullOrEmpty(headerValue))
        {
            return _next.Invoke(context);
        }

        try
        {
            var bytes = Convert.FromBase64String(headerValue);

            var correlationContext = correlationContextSerializer.Deserialize(bytes);

            if (correlationContext is not null)
            {
                correlationContextAccessor.CorrelationContext = correlationContext;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize CorrelationContext from header");
        }

        return _next.Invoke(context);
    }
}
