using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.Crud;
using BiUM.Specialized.Services.Crud;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
    public async Task<ApiResponse> SaveAsync(string code, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var response = await _crudService.SaveAsync(code, body.ToDictionary(), cancellationToken);

        return response;
    }

    [HttpDelete("{code}")]
    public async Task<ApiResponse> DeleteAsync(string code, [FromBody] DeleteCrudCommand command, CancellationToken cancellationToken)
    {
        return await _crudService.DeleteAsync(code, command.Id!.Value, false, cancellationToken);
    }

    [HttpGet("{code}")]
    public async Task<ApiResponse<IDictionary<string, object?>>> GetAsync(string code, string id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse<IDictionary<string, object?>>();

        Guid.TryParse(id, out var guidId);

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
}
