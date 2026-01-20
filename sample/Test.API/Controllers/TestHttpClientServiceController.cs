using BiUM.Core.HttpClients;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.API.Controllers;

[BiUMRoute("test")]
public class TestHttpClientServiceController : ApiControllerBase
{
    private readonly IHttpClientsService _httpClientsService;

    public TestHttpClientServiceController(IHttpClientsService httpClientsService)
    {
        _httpClientsService = httpClientsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLanguages([FromQuery] GetQuery query, CancellationToken cancellationToken = default)
    {
        var languages = await _httpClientsService.CallService<object>(Guid.Parse("2fdfbfaa-ca10-5142-9af4-112244eea8b4"), cancellationToken: cancellationToken);

        if (languages is not null && languages.Success)
        {
            return Ok(new { Key = "Success", languages.Value });
        }

        return NotFound($"Service Call is not success.");
    }


    [HttpGet]
    public async Task<IActionResult> GetVgenAssetGroups([FromQuery] GetQuery query, CancellationToken cancellationToken = default)
    {
        var assetGroups = await _httpClientsService.CallService<object>(Guid.Parse("63f1b159-19ba-43e0-b74d-5cb80281ab3e"), cancellationToken: cancellationToken);

        if (assetGroups is not null && assetGroups.Success)
        {
            return Ok(new { Key = "Success", assetGroups.Value });
        }

        return NotFound($"Service Call is not success.");
    }
}

public class GetQuery
{
}
