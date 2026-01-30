using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.Crud;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.Crud;

public interface ICrudService
{
    Task<ApiResponse> SaveAsync(
        string code,
        Dictionary<string, object?> data,
        CancellationToken cancellationToken);

    Task<ApiResponse> DeleteAsync(string code, Guid id, bool hardDelete, CancellationToken cancellationToken);

    Task<IDictionary<string, object?>> GetAsync(string code, Guid id, CancellationToken cancellationToken);

    Task<PaginatedApiResponse<IDictionary<string, object?>>> GetListAsync(
        string code,
        Dictionary<string, string> query,
        CancellationToken cancellationToken);

    Task<ApiResponse> PublishDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApiResponse> SaveDomainCrudAsync(
        SaveDomainCrudCommand command,
        CancellationToken cancellationToken);

    Task<ApiResponse> DeleteDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApiResponse<DomainCrudDto>> GetDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApiResponse<DomainCrudDto>> GetDomainCrudByCodeAsync(
        string code,
        CancellationToken cancellationToken);

    Task<PaginatedApiResponse<DomainCrudsDto>> GetDomainCrudsAsync(
        Guid? applicationId,
        string? name,
        string? code,
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken);
}