using BiUM.Core.Models;
using BiUM.Infrastructure.Services.Authorization;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BiUM.Specialized.Services.Authorization;

public class CorrelationContextProvider : ICorrelationContextProvider
{
    private const string CorrelationContextHeader = "X-Correlation-Context";

    private static readonly MessagePackSerializerOptions MessagePackOptions =
        StandardResolver.Options
            .WithOmitAssemblyVersion(true)
            .WithAllowAssemblyVersionMismatch(true)
            .WithCompression(MessagePackCompression.Lz4BlockArray);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CorrelationContextProvider> _logger;

    public CorrelationContextProvider(IHttpContextAccessor httpContextAccessor, ILogger<CorrelationContextProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public CorrelationContext? Get()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return null;
        }

        var headerValue = httpContext.Request.Headers[CorrelationContextHeader].ToString();

        if (string.IsNullOrEmpty(headerValue))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(headerValue);

            return MessagePackSerializer.Deserialize<CorrelationContext>(bytes, MessagePackOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize CorrelationContext from header.");

            return null;
        }
    }
}