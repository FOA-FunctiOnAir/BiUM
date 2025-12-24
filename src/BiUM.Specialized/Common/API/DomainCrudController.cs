using BiUM.Specialized.Common.Crud;
using BiUM.Specialized.Services.Crud;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

[ApiController]
[BiUMBaseRoute]
public class DomainCrudController : ApiControllerBase
{
    private readonly ICrudService _crudService;

    public DomainCrudController(ICrudService crudService)
    {
        _crudService = crudService;
    }

    [HttpPost]
    public async Task<ApiEmptyResponse> PublishDomainCrudAsync([FromBody] PublishDomainCrudCommand command, CancellationToken cancellationToken)
    {
        var response = await _crudService.PublishDomainCrudAsync(command.Id!.Value, cancellationToken);

        return response;
    }

    [HttpPost]
    public async Task<ApiEmptyResponse> SaveDomainCrudAsync([FromBody] SaveDomainCrudCommand command, CancellationToken cancellationToken)
    {
        var response = await _crudService.SaveDomainCrudAsync(command, cancellationToken);

        return response;
    }

    [HttpDelete]
    public async Task<ApiEmptyResponse> DeleteDomainCrudAsync([FromBody] DeleteDomainCrudCommand command, CancellationToken cancellationToken)
    {
        var response = await _crudService.DeleteDomainCrudAsync(command.Id!.Value, cancellationToken);

        return response;
    }

    [HttpGet]
    public async Task<ApiResponse<DomainCrudDto>> GetDomainCrudAsync(string id, CancellationToken cancellationToken)
    {
        _ = Guid.TryParse(id, out var guidId);

        var response = await _crudService.GetDomainCrudAsync(guidId, cancellationToken);

        return response;
    }

    [HttpGet]
    public async Task<ApiResponse<DomainCrudDto>> GetDomainCrudByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var response = await _crudService.GetDomainCrudByCodeAsync(code, cancellationToken);

        return response;
    }

    [HttpGet]
    public async Task<PaginatedApiResponse<DomainCrudsDto>> GetDomainCrudsAsync([FromQuery] GetDomainCrudsQuery query, CancellationToken cancellationToken)
    {

        var response = await _crudService.GetDomainCrudsAsync(query.Name, query.Code, query.Q, query.PageStart, query.PageSize, cancellationToken);

        return response;
    }
}