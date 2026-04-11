using BiUM.Contract.Enums;
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
    public Task<ApiResponse> SaveAsync(
        string code,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        return _crudService.SaveAsync(code, body.ToDictionary(), cancellationToken);
    }

    [HttpPost("{code}/{partialCode}")]
    public async Task<ApiResponse> SavePartialAsync(
        string code,
        string partialCode,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        var dict = body.ToDictionary();

        if (!dict.TryGetValue("Id", out var idObj) || idObj is null)
        {
            var r = new ApiResponse();

            r.AddMessage(new ResponseMessage
            {
                Code = "id_required",
                Message = "Body must include Id.",
                Severity = MessageSeverity.Error
            });

            return r;
        }

        Guid id;

        if (idObj is Guid g)
        {
            id = g;
        }
        else if (!Guid.TryParse(idObj.ToString(), out id) || id == Guid.Empty)
        {
            var r = new ApiResponse();

            r.AddMessage(new ResponseMessage
            {
                Code = "id_invalid",
                Message = "Id must be a valid GUID.",
                Severity = MessageSeverity.Error
            });

            return r;
        }

        _ = dict.Remove("Id");

        return await _crudService.SavePartialAsync(code, partialCode, id, dict, cancellationToken);
    }

    [HttpDelete("{code}")]
    public Task<ApiResponse> DeleteAsync(
        string code,
        [FromBody] DeleteCrudCommand command,
        CancellationToken cancellationToken)
    {
        return _crudService.DeleteAsync(code, command.Id!.Value, false, cancellationToken);
    }

    [HttpGet("{code}")]
    public async Task<ApiResponse> GetAsync(
        [FromRoute] string code,
        [FromQuery] Dictionary<string, string>? query,
        CancellationToken cancellationToken)
    {
        query ??= new Dictionary<string, string>();

        if (query.TryGetValue("id", out var id) && !string.IsNullOrEmpty(id))
        {
            return await GetAsync(code, id, cancellationToken);
        }

        return await GetListAsync(code, query, cancellationToken);
    }

    private async Task<ApiResponse<IDictionary<string, object?>>> GetAsync(
        [FromRoute] string code,
        [FromQuery] string id,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse<IDictionary<string, object?>>();

        Guid guid = Guid.TryParse(id, out guid) ? guid : Guid.Empty;

        response.Value = await _crudService.GetAsync(code, guid, cancellationToken);

        return response;
    }

    private Task<PaginatedApiResponse<IDictionary<string, object?>>> GetListAsync(
        [FromRoute] string code,
        [FromQuery] Dictionary<string, string> query,
        CancellationToken cancellationToken)
    {
        return _crudService.GetListAsync(code, query, cancellationToken);
    }
}