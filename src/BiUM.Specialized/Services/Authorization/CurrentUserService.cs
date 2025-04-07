using BiUM.Core.Consts;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Specialized.Consts;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BiUM.Specialized.Services.Authorization;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private string? userId => _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? identity => _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.SerialNumber);

    public Guid? UserId => !string.IsNullOrWhiteSpace(userId) ? Guid.Parse(userId) : null;
    public string Identity => identity ?? string.Empty;

    public Guid CorrelationId => GetCorrelationId();

    public Guid? ApplicationId => GetApplicationId();

    public Guid? TenantId => GetTenantId();

    public Guid LanguageId => GetLanguageId();

    public string Token => GetToken();

    private Guid GetCorrelationId()
    {
        if (_httpContextAccessor?.HttpContext?.Request.Headers.ContainsKey(HeaderKeys.CorrelationId) == true)
        {
            try
            {
                var correlation = _httpContextAccessor?.HttpContext?.Request.Headers[HeaderKeys.CorrelationId];

                return Guid.Parse(correlation.ToString()!);
            }
            catch { }
        }

        return Guid.NewGuid();
    }

    private Guid? GetApplicationId()
    {
        if (_httpContextAccessor?.HttpContext?.Request.Headers.ContainsKey(HeaderKeys.ApplicationId) == true)
        {
            try
            {
                var application = _httpContextAccessor?.HttpContext?.Request.Headers[HeaderKeys.ApplicationId];

                return Guid.Parse(application.ToString()!);
            }
            catch { }
        }

        return null;
    }

    private Guid? GetTenantId()
    {
        if (_httpContextAccessor?.HttpContext?.Request.Headers.ContainsKey(HeaderKeys.TenantId) == true)
        {
            try
            {
                var tenantId = _httpContextAccessor?.HttpContext?.Request.Headers[HeaderKeys.TenantId];

                return Guid.Parse(tenantId.ToString()!);
            }
            catch { }
        }

        return null;
    }

    private Guid GetLanguageId()
    {
        if (_httpContextAccessor?.HttpContext?.Request.Headers.ContainsKey(HeaderKeys.LanguageId) == true)
        {
            try
            {
                var language = _httpContextAccessor?.HttpContext?.Request.Headers[HeaderKeys.LanguageId];

                return Guid.Parse(language.ToString()!);
            }
            catch { }
        }

        return Ids.Language.Turkish.Id;
    }

    private string GetToken()
    {
        if (_httpContextAccessor?.HttpContext?.Request.Headers.ContainsKey(HeaderKeys.AuthorizationToken) == true)
        {
            try
            {
                var token = _httpContextAccessor?.HttpContext?.Request.Headers[HeaderKeys.AuthorizationToken];

                return token;
            }
            catch { }
        }

        return null;
    }
}