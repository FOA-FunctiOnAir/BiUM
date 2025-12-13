using BiUM.Core.Consts;
using BiUM.Core.Models;
using BiUM.Infrastructure.Services.Authorization;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BiUM.Specialized.Services.Authorization;

/// <summary>
/// This class provides functionality to retrieve and deserialize the CorrelationContext
/// from the HTTP request headers. It has to be registered as a scoped service to ensure
/// that each HTTP request gets its own instance.
/// </summary>
public class CorrelationContextProvider : ICorrelationContextProvider
{
    private static readonly MessagePackSerializerOptions MessagePackOptions =
        StandardResolver.Options
            .WithOmitAssemblyVersion(true)
            .WithAllowAssemblyVersionMismatch(true)
            .WithCompression(MessagePackCompression.Lz4BlockArray);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CorrelationContextProvider> _logger;

    private CorrelationContext? _correlationContext;

    public CorrelationContextProvider(IHttpContextAccessor httpContextAccessor, ILogger<CorrelationContextProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public CorrelationContext? Get()
    {
        if (_correlationContext is not null)
        {
            return _correlationContext;
        }

        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return null;
        }

        var headerValue = httpContext.Request.Headers[HeaderKeys.CorrelationContext].ToString();

        if (string.IsNullOrEmpty(headerValue))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(headerValue);

            _correlationContext = MessagePackSerializer.Deserialize<CorrelationContext>(bytes, MessagePackOptions);

            return _correlationContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize CorrelationContext from header.");

            return null;
        }
    }
}