using BiUM.Specialized.Common.Crud;
using BiUM.Specialized.Services.Crud;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BiUM.Specialized.Common.API;

[ApiController]
[BiUMBaseRoute]
public class CrudController : ApiControllerBase
{
    private readonly ICrudService _crudService;

    public CrudController(ICrudService crudService)
    {
        _crudService = crudService;
    }

    [HttpPost("{code}")]
    public async Task<ApiEmptyResponse> SaveAsync(string code, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var response = await _crudService.SaveAsync(code, body.ToDictionary(), cancellationToken);

        return response;
    }

    [HttpDelete("{code}")]
    public async Task<ApiEmptyResponse> DeleteAsync(string code, [FromBody] DeleteCrudCommand command, CancellationToken cancellationToken)
    {
        return await _crudService.DeleteAsync(code, command.Id!.Value, false, cancellationToken);
    }

    [HttpGet("{code}")]
    public async Task<ApiResponse<IDictionary<string, object?>>> GetAsync(string code, string id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse<IDictionary<string, object?>>();

        _ = Guid.TryParse(id, out Guid guidId);

        var responseGet = await _crudService.GetAsync(code, guidId, cancellationToken);

        response.Value = responseGet;

        return response;
    }

    [HttpGet("{code}")]
    public async Task<PaginatedApiResponse<IDictionary<string, object?>>> GetListAsync(
        string code,
        [FromQuery] Dictionary<string, string> query,
        CancellationToken cancellationToken)
    {
        var response = await _crudService.GetListAsync(code, query, cancellationToken);

        return response;
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
        Guid.TryParse(id, out var guidId);

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