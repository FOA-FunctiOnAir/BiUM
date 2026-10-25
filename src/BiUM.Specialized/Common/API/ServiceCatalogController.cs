using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Attributes;
using BiUM.Specialized.Services.ServiceCatalog;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<PaginatedApiResponse<ServiceCatalogEndpointDto>> GetEndpoints([FromQuery] ServiceCatalogEndpointsQuery query, CancellationToken cancellationToken)
    {
        return await _serviceCatalogService.GetEndpointsAsync(query, cancellationToken);
    }

    [HttpGet]
    public async Task<ApiResponse<ServiceCatalogManifestDto>> GetManifest([FromQuery] string? httpMethod, [FromQuery] string? url, CancellationToken cancellationToken)
    {
        return await _serviceCatalogService.GetManifestAsync(httpMethod, url, cancellationToken);
    }

    [HttpPost]
    public async Task<ApiResponse<ServiceCatalogPreviewStartedDto>> Preview([FromBody] ServiceCatalogPreviewCommand command, CancellationToken cancellationToken)
    {
        return await _serviceCatalogService.PreviewAsync(command, cancellationToken);
    }

    [HttpPost]
    public async Task<ApiResponse<JsonElement>> Apply([FromBody] ServiceCatalogApplyCommand command, CancellationToken cancellationToken)
    {
        return await _serviceCatalogService.ApplyAsync(command, cancellationToken);
    }
}