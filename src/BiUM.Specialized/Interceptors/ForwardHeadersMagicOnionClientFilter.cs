using MagicOnion.Client;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace BiUM.Specialized.Interceptors;

internal sealed class ForwardHeadersMagicOnionClientFilter : IClientFilter
{
    private static readonly string[] AllowedHeaderKeys =
    [
        "accept-language",
        "x-correlation-context"
    ];

    private static readonly string[] BlockedHeaderKeys =
    [
        "host",
        "content-type",
        "content-length",
        "grpc-timeout",
        "te",
        "user-agent",
        ":authority",
        ":method",
        ":path",
        ":scheme"
    ];

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardHeadersMagicOnionClientFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
    {
        var headers = context.CallOptions.Headers ?? [];

        var httpHeaders = _httpContextAccessor.HttpContext?.Request.Headers;

        if (httpHeaders is null)
        {
            return next.Invoke(context);
        }

        foreach (var header in httpHeaders)
        {
            var key = header.Key.ToLowerInvariant();

            var blocked = BlockedHeaderKeys.Contains(key);

            if (blocked)
            {
                continue;
            }

            var allowed = AllowedHeaderKeys.Contains(key);

            if (allowed)
            {
                headers.Add(key, header.Value.ToString());
            }
        }

        context.CallOptions.WithHeaders(headers);

        return next.Invoke(context);
    }
}
