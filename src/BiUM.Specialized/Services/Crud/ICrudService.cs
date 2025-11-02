using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.Crud;

namespace BiUM.Specialized.Services.Crud;

public interface ICrudService
{
    Task<ApiEmptyResponse> SaveAsync(
        string code,
        Dictionary<string, object?> data,
        CancellationToken cancellationToken);

    Task<ApiEmptyResponse> DeleteAsync(string code, Guid id, bool hardDelete, CancellationToken cancellationToken);

    Task<IDictionary<string, object?>> GetAsync(string code, Guid id, CancellationToken cancellationToken);

    Task<PaginatedApiResponse<IDictionary<string, object?>>> GetListAsync(
        string code,
        Dictionary<string, string> query,
        CancellationToken cancellationToken);

    Task<ApiEmptyResponse> PublishDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApiEmptyResponse> SaveDomainCrudAsync(
        SaveDomainCrudCommand command,
        CancellationToken cancellationToken);

    Task<ApiEmptyResponse> DeleteDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApiResponse<DomainCrudDto>> GetDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApiResponse<DomainCrudDto>> GetDomainCrudByCodeAsync(
        string code,
        CancellationToken cancellationToken);

    Task<PaginatedApiResponse<DomainCrudsDto>> GetDomainCrudsAsync(
        string? name,
        string? code,
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken);
}