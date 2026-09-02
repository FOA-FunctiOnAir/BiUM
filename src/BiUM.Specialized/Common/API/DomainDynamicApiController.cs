using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.DynamicApi;
using BiUM.Specialized.Services.DynamicApi;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

[ApiController]
[BiUMBaseRoute]
public class DomainDynamicApiController : ApiControllerBase
{
    private readonly IDynamicApiService _dynamicApiService;

    public DomainDynamicApiController(IDynamicApiService dynamicApiService)
    {
        _dynamicApiService = dynamicApiService;
    }

    [HttpPost]
    [CompensatableApi]
    public Task<ApiResponse> PublishDomainDynamicApiAsync([FromBody] PublishDomainDynamicApiCommand command, CancellationToken cancellationToken)
    {
        return _dynamicApiService.PublishDomainDynamicApiAsync(command.Id!.Value, cancellationToken);
    }

    [HttpPost]
    [CompensatableApi]
    public Task<ApiResponse> SaveDomainDynamicApiAsync([FromBody] SaveDomainDynamicApiCommand command, CancellationToken cancellationToken)
    {
        return _dynamicApiService.SaveDomainDynamicApiAsync(command, cancellationToken);
    }

    [HttpDelete]
    [CompensatableApi]
    public Task<ApiResponse> DeleteDomainDynamicApiAsync([FromBody] DeleteDomainDynamicApiCommand command, CancellationToken cancellationToken)
    {
        return _dynamicApiService.DeleteDomainDynamicApiAsync(command.Id!.Value, cancellationToken);
    }

    [HttpGet]
    public Task<ApiResponse<DomainDynamicApiDto>> GetDomainDynamicApiAsync(string id, CancellationToken cancellationToken)
    {
        Guid.TryParse(id, out var guidId);
        return _dynamicApiService.GetDomainDynamicApiAsync(guidId, cancellationToken);
    }

    [HttpGet]
    public Task<ApiResponse<DomainDynamicApiDto>> GetDomainDynamicApiByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return _dynamicApiService.GetDomainDynamicApiByCodeAsync(code, cancellationToken);
    }

    [HttpGet]
    public Task<PaginatedApiResponse<DomainDynamicApisDto>> GetDomainDynamicApisAsync([FromQuery] GetDomainDynamicApisQuery query, CancellationToken cancellationToken)
    {
        return _dynamicApiService.GetDomainDynamicApisAsync(
            query.ApplicationId,
            query.Name,
            query.Code,
            query.Q,
            query.PageStart,
            query.PageSize,
            cancellationToken);
    }

    [HttpGet]
    public Task<ApiResponse<IReadOnlyList<DynamicApiSelectableTableDto>>> GetDynamicApiSelectableTablesAsync(
        [FromQuery] GetDynamicApiSelectableTablesQuery query,
        CancellationToken cancellationToken)
    {
        return _dynamicApiService.GetDynamicApiSelectableTablesAsync(
            query.ApplicationId,
            query.MicroserviceId,
            cancellationToken);
    }

    [HttpGet]
    public Task<ApiResponse<IReadOnlyList<DomainDynamicApisByTableDto>>> GetDomainDynamicApisByTableAsync(
        [FromQuery] GetDomainDynamicApisByTableQuery query,
        CancellationToken cancellationToken)
    {
        return _dynamicApiService.GetDomainDynamicApisByTableAsync(
            query.Schema,
            query.TableName,
            cancellationToken);
    }
}