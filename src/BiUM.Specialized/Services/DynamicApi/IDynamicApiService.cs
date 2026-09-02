using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.DynamicApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public interface IDynamicApiService
{
    Task<ApiResponse> PublishDomainDynamicApiAsync(Guid id, CancellationToken cancellationToken);

    Task<ApiResponse> SaveDomainDynamicApiAsync(SaveDomainDynamicApiCommand command, CancellationToken cancellationToken);

    Task<ApiResponse> DeleteDomainDynamicApiAsync(Guid id, CancellationToken cancellationToken);

    Task<ApiResponse<DomainDynamicApiDto>> GetDomainDynamicApiAsync(Guid id, CancellationToken cancellationToken);

    Task<ApiResponse<DomainDynamicApiDto>> GetDomainDynamicApiByCodeAsync(string code, CancellationToken cancellationToken);

    Task<PaginatedApiResponse<DomainDynamicApisDto>> GetDomainDynamicApisAsync(
        Guid? applicationId,
        string? name,
        string? code,
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken);

    Task<object> ExecuteAsync(
        string code,
        Guid expectedHttpType,
        Dictionary<string, object?> parameters,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken);

    Task<bool> IsDynamicApiMutationCompensatibleByCodeAsync(string code, CancellationToken cancellationToken);

    Task<ApiResponse<IReadOnlyList<DynamicApiSelectableTableDto>>> GetDynamicApiSelectableTablesAsync(
        Guid applicationId,
        Guid microserviceId,
        CancellationToken cancellationToken);

    Task<ApiResponse<IReadOnlyList<DomainDynamicApisByTableDto>>> GetDomainDynamicApisByTableAsync(
        string schema,
        string tableName,
        CancellationToken cancellationToken);
}