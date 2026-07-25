using BiUM.Core.Authorization;
using BiUM.Core.Constants;
using BiUM.Core.Serialization;
using MagicOnion.Client;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.MagicOnion.Filters.Client;

internal sealed class CorrelationContextFilter : IClientFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly ICorrelationContextSerializer _correlationContextSerializer;

    public CorrelationContextFilter(
        IHttpContextAccessor httpContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ICorrelationContextSerializer correlationContextSerializer)
    {
        _httpContextAccessor = httpContextAccessor;
        _correlationContextAccessor = correlationContextAccessor;
        _correlationContextSerializer = correlationContextSerializer;
    }

    public ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
    {
        var headers = context.CallOptions.Headers ?? [];

        if (headers.Any(e => string.Equals(e.Key, HeaderKeys.CorrelationContext, StringComparison.OrdinalIgnoreCase)))
        {
            return next.Invoke(context);
        }

        // Prefer in-memory accessor: it holds the most current context for this request
        // (set by CorrelationContextMiddleware before any gRPC call), avoiding a redundant
        // HTTP header serialize on the hot path when cache is warm.
        var correlationContext = _correlationContextAccessor.CorrelationContext;

        if (correlationContext is not null)
        {
            var bytes = _correlationContextSerializer.Serialize(correlationContext);

            var base64 = Convert.ToBase64String(bytes);

            headers.Add(HeaderKeys.CorrelationContext, base64);
        }
        else
        {
            // Fallback: read from HTTP request header (e.g. background jobs or
            // calls made outside a CorrelationContextMiddleware pipeline).
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is not null)
            {
                var correlationContextHeader = httpContext.Request.Headers[HeaderKeys.CorrelationContext].ToString();

                if (!string.IsNullOrEmpty(correlationContextHeader))
                {
                    headers.Add(HeaderKeys.CorrelationContext, correlationContextHeader);
                }
            }
        }

        context.CallOptions.WithHeaders(headers);

        return next.Invoke(context);
    }
}