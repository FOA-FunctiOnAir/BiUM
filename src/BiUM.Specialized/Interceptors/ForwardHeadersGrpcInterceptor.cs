using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace BiUM.Specialized.Interceptors;

public class ForwardHeadersGrpcInterceptor : Interceptor
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

    public ForwardHeadersGrpcInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var newContext = WithForwardedHeaders(context);

        return continuation.Invoke(request, newContext);
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var newContext = WithForwardedHeaders(context);

        return continuation.Invoke(request, newContext);
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var newContext = WithForwardedHeaders(context);

        return continuation.Invoke(request, newContext);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var newContext = WithForwardedHeaders(context);

        return continuation.Invoke(newContext);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var newContext = WithForwardedHeaders(context);

        return continuation.Invoke(newContext);
    }

    private ClientInterceptorContext<TRequest, TResponse> WithForwardedHeaders<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        var headers = context.Options.Headers ?? [];

        var httpHeaders = _httpContextAccessor.HttpContext?.Request.Headers;

        if (httpHeaders is not null)
        {
            foreach (var header in httpHeaders)
            {
                var key = header.Key.ToLowerInvariant();

                var blocked = BlockedHeaderKeys.Contains(key);

                if (blocked)
                {
                    continue;
                }

                var allowed = AllowedHeaderKeys.Length == 0 || AllowedHeaderKeys.Contains(key);

                if (allowed)
                {
                    headers.Add(key, header.Value.ToString());
                }
            }
        }

        var opts = context.Options.WithHeaders(headers);

        return new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, opts);
    }
}
