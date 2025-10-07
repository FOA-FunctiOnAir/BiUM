using BiUM.Core.Consts;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;

namespace BiUM.Specialized.Interceptors;

public class ForwardHeadersGrpcInterceptor : Interceptor
{
    private readonly IHttpContextAccessor _http;

    private static readonly string[] AllowedHeaderKeys =
    {
        "accept-language",
        HeaderKeys.AuthorizationToken.ToLowerInvariant(),
        HeaderKeys.ApplicationId.ToLowerInvariant(),
        HeaderKeys.LanguageId.ToLowerInvariant(),
        HeaderKeys.CorrelationId.ToLowerInvariant(),
        HeaderKeys.TenantId.ToLowerInvariant()
    };

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

    public ForwardHeadersGrpcInterceptor(IHttpContextAccessor http)
    {
        _http = http;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var ctx2 = WithForwardedHeaders(context);

        return continuation(request, ctx2);
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var ctx2 = WithForwardedHeaders(context);

        return continuation(request, ctx2);
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var ctx2 = WithForwardedHeaders(context);

        return continuation(request, ctx2);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var ctx2 = WithForwardedHeaders(context);

        return continuation(ctx2);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var ctx2 = WithForwardedHeaders(context);

        return continuation(ctx2);
    }

    private ClientInterceptorContext<TRequest, TResponse> WithForwardedHeaders<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        var md = context.Options.Headers ?? new Metadata();

        var httpHeaders = _http.HttpContext?.Request?.Headers;

        if (httpHeaders is not null)
        {
            foreach (var header in httpHeaders)
            {
                var key = header.Key.ToLowerInvariant();

                var allowed = AllowedHeaderKeys.Contains(key);

                var blocked = BlockedHeaderKeys.Contains(key);

                if ((AllowedHeaderKeys.Length == 0 || allowed) && !blocked)
                {
                    md.Add(key, header.Value.ToString());
                }
            }
        }

        var opts = context.Options.WithHeaders(md);

        return new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, opts);
    }
}