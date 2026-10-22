using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Attributes;
using BiUM.Specialized.Services.ServiceCatalog;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

[ApiController]
[BiUMBaseRoute]
[ServiceCatalog(false)]
public class ServiceCatalogController : ApiControllerBase
{
    private readonly IServiceCatalogService _serviceCatalogService;

    public ServiceCatalogController(IServiceCatalogService serviceCatalogService)
    {
        _serviceCatalogService = serviceCatalogService;
    }

    [HttpGet]
    public ApiResponse<IList<ServiceCatalogEndpointDto>> GetEndpoints()
    {
        return _serviceCatalogService.GetEndpoints();
    }

    [HttpGet]
    public async Task<ApiResponse<ServiceCatalogManifestDto>> GetManifest([FromQuery] string? httpMethod, [FromQuery] string? url, CancellationToken cancellationToken)
    {
        return await _serviceCatalogService.GetManifestAsync(httpMethod, url, cancellationToken);
    }

    [HttpPost]
    public async Task<ApiResponse<JsonElement>> Preview([FromBody] ServiceCatalogPreviewCommand command, CancellationToken cancellationToken)
    {
        return await _serviceCatalogService.PreviewAsync(command, cancellationToken);
    }

    [HttpPost]
    public async Task<ApiResponse<JsonElement>> Apply([FromBody] ServiceCatalogApplyCommand command, CancellationToken cancellationToken)
    {
        return await _serviceCatalogService.ApplyAsync(command, cancellationToken);
    }
}