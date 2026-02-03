using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Configs;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.MagicOnion.Filters.Server;

[SuppressMessage("Design", "CA1019: Define accessors for attribute arguments", Justification = "Dependencies are injected via constructor")]
public class GlobalApiResponseFilter : MagicOnionFilterAttribute
{
    private const string UnhandledExceptionOccurred = "An unhandled exception occurred";

    private static readonly ConcurrentDictionary<MethodInfo, Func<ServiceContext, Exception, bool, object>?> _responseFactoryCache = new();

    private readonly ILogger<GlobalApiResponseFilter> _logger;

    private readonly bool _isNotProductionLike;

    public GlobalApiResponseFilter(
        IHostEnvironment environment,
        IOptions<BiAppOptions> appOptionsAccessor,
        ILogger<GlobalApiResponseFilter> logger)
    {
        _logger = logger;

        _isNotProductionLike =
            environment.IsDevelopment() ||
            appOptionsAccessor.Value is not { Environment: "Production" or "Sandbox" or "Staging" or "QA" };

        Order = 0;
    }

    public override async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing gRPC request. Method: {Method}.", context.MethodInfo.Name);

            var responseFactory = _responseFactoryCache.GetOrAdd(context.MethodInfo, CreateResponseFactory);

            if (responseFactory is not null)
            {
                var response = responseFactory.Invoke(context, ex, _isNotProductionLike);

                context.SetRawResponse(response);
            }
            else
            {
                throw;
            }
        }
    }

    private static Func<ServiceContext, Exception, bool, object>? CreateResponseFactory(MethodInfo methodInfo)
    {
        var returnType = methodInfo.ReturnType;

        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(UnaryResult<>))
        {
            return null;
        }

        var responseType = returnType.GetGenericArguments()[0];

        if (!typeof(ApiResponse).IsAssignableFrom(responseType))
        {
            return null;
        }

        return (context, exception, isNotProductionLike) =>
        {
            var responseInstance = (ApiResponse)Activator.CreateInstance(responseType)!;

            SetStatusCode(context, StatusCode.OK, string.Empty);

            if (isNotProductionLike)
            {
                responseInstance.AddMessage(
                    code: GetCode(exception),
                    message: exception.Message,
                    exception: exception.ToString(),
                    severity: MessageSeverity.Error
                );
            }
            else
            {
                responseInstance.AddMessage(
                    code: GetCode(exception),
                    message: UnhandledExceptionOccurred,
                    exception: exception.Message,
                    severity: MessageSeverity.Error
                );
            }

            return responseInstance;
        };
    }

    private static string GetCode(Exception exception)
    {
        var type = exception.GetType().Name;

        if (type.EndsWith(nameof(Exception), StringComparison.Ordinal))
        {
            type = type.Remove(type.Length - nameof(Exception).Length);
        }

        return type.ToSnakeCase();
    }
}