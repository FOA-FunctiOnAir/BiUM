using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Core.Consts;
using BiUM.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;

namespace BiUM.Infrastructure.Services.Authorization;

internal sealed class CorrelationContextProvider : ICorrelationContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly ICorrelationContextSerializer _correlationContextSerializer;
    private readonly ILogger<CorrelationContextProvider> _logger;

    private CorrelationContext? _correlationContext;

    public CorrelationContextProvider(
        IHttpContextAccessor httpContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ICorrelationContextSerializer correlationContextSerializer,
        ILogger<CorrelationContextProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _correlationContextAccessor = correlationContextAccessor;
        _correlationContextSerializer = correlationContextSerializer;
        _logger = logger;
    }

    public CorrelationContext? Get()
    {
        if (_correlationContext is not null)
        {
            return _correlationContext;
        }

        if (_correlationContextAccessor.CorrelationContext is not null)
        {
            _correlationContext = _correlationContextAccessor.CorrelationContext;

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

            _correlationContext = _correlationContextSerializer.Deserialize(bytes);

            return _correlationContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize CorrelationContext from header");

            return null;
        }
    }
}
