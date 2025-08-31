using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;

namespace BiUM.Infrastructure.Common.Interceptors;

public class ForwardHeadersGrpcInterceptor : Interceptor
{
    private readonly IHttpContextAccessor _http;

    public ForwardHeadersGrpcInterceptor(IHttpContextAccessor http) => _http = http;

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var md = context.Options.Headers ?? new Metadata();

        var headers = _http.HttpContext?.Request?.Headers;

        if (headers != null)
        {
            foreach (var header in headers)
            {
                md.Add(header.Key, header.Value.ToString());
            }
        }

        var opts = context.Options.WithHeaders(md);

        var ctx2 = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, opts);

        return base.AsyncUnaryCall(request, ctx2, continuation);
    }
}