namespace BiUM.Infrastructure.Services.Authorization;

public interface ICurrentUserService
{
    Guid? UserId { get; }
}