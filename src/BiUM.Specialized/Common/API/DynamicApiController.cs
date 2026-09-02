using BiUM.Core.Constants;
using BiUM.Specialized.Services.DynamicApi;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

[ApiController]
[BiUMBaseRoute]
public class DynamicApiController : ApiControllerBase
{
    private readonly IDynamicApiService _dynamicApiService;

    public DynamicApiController(IDynamicApiService dynamicApiService)
    {
        _dynamicApiService = dynamicApiService;
    }

    [HttpGet("{code}")]
    public Task<object> Get(string code, [FromQuery] Dictionary<string, string>? query, CancellationToken cancellationToken)
    {
        query ??= new Dictionary<string, string>();
        var (pageStart, pageSize, parameters) = ParseQuery(query);
        return _dynamicApiService.ExecuteAsync(
            code,
            Ids.Parameter.HttpType.Values.Get,
            parameters,
            pageStart,
            pageSize,
            cancellationToken);
    }

    [HttpPost("{code}")]
    public Task<object> Post(string code, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        return _dynamicApiService.ExecuteAsync(
            code,
            Ids.Parameter.HttpType.Values.Post,
            body.ToDictionary(),
            null,
            null,
            cancellationToken);
    }

    [HttpPut("{code}")]
    public Task<object> Put(string code, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        return _dynamicApiService.ExecuteAsync(
            code,
            Ids.Parameter.HttpType.Values.Put,
            body.ToDictionary(),
            null,
            null,
            cancellationToken);
    }

    [HttpPatch("{code}")]
    public Task<object> Patch(string code, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        return _dynamicApiService.ExecuteAsync(
            code,
            Ids.Parameter.HttpType.Values.Patch,
            body.ToDictionary(),
            null,
            null,
            cancellationToken);
    }

    [HttpDelete("{code}")]
    public Task<object> Delete(string code, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        return _dynamicApiService.ExecuteAsync(
            code,
            Ids.Parameter.HttpType.Values.Delete,
            body.ToDictionary(),
            null,
            null,
            cancellationToken);
    }

    private static (int? PageStart, int? PageSize, Dictionary<string, object?> Parameters) ParseQuery(Dictionary<string, string> query)
    {
        int? pageStart = null;
        int? pageSize = null;
        var parameters = new Dictionary<string, object?>();

        foreach (var (key, value) in query)
        {
            if (key.Equals("pageStart", System.StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(value, out var ps))
                {
                    pageStart = ps;
                }

                continue;
            }

            if (key.Equals("pageSize", System.StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(value, out var psz))
                {
                    pageSize = psz;
                }

                continue;
            }

            parameters[key] = value;
        }

        return (pageStart, pageSize, parameters);
    }
}