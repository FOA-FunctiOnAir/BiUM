namespace BiUM.Infrastructure.Services.Authorization;

public interface ICurrentUserService
{
    Guid CorrelationId { get; }
    Guid? ApplicationId { get; }
    Guid? TenantId { get; }
    Guid UserId { get; }
    string Identity { get; }
    Guid LanguageId { get; }
    string Token { get; }
}