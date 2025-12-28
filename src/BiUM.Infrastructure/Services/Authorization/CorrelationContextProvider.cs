using BiUM.Core.Authorization;
using BiUM.Core.Consts;
using BiUM.Core.Models;
using BiUM.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;

namespace BiUM.Infrastructure.Services.Authorization;

/// <summary>
/// This class provides functionality to retrieve and deserialize the CorrelationContext
/// from the HTTP request headers. It has to be registered as a scoped service to ensure
/// that each HTTP request gets its own instance.
/// </summary>
public class CorrelationContextProvider : ICorrelationContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICorrelationContextSerializer _correlationContextSerializer;
    private readonly ILogger<CorrelationContextProvider> _logger;

    private CorrelationContext? _correlationContext;

    public CorrelationContextProvider(IHttpContextAccessor httpContextAccessor, ICorrelationContextSerializer correlationContextSerializer, ILogger<CorrelationContextProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _correlationContextSerializer = correlationContextSerializer;
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
            _correlationContext = _correlationContextSerializer.Deserialize(headerValue);

            return _correlationContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize CorrelationContext from header.");

            return null;
        }
    }
}
