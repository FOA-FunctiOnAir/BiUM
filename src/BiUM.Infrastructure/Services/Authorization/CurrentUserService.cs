using System.Security.Claims;

using Microsoft.AspNetCore.Http;

namespace BiUM.Infrastructure.Services.Authorization;

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

    public Guid? TenantId => GetTenantId();

    private Guid? GetTenantId()
    {
        if (_httpContextAccessor?.HttpContext?.Request.Headers.ContainsKey("TenantId") == true)
        {
            var a = _httpContextAccessor?.HttpContext?.Request.Headers["TenantId"];

            return Guid.Parse(a.ToString()!);
        }

        return null;
    }
}