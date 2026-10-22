using BiUM.Contract.Models.Api;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.ServiceCatalog;

public interface IServiceCatalogService
{
    ApiResponse<IList<ServiceCatalogEndpointDto>> GetEndpoints();

    Task<ApiResponse<ServiceCatalogManifestDto>> GetManifestAsync(string? httpMethod, string? url, CancellationToken cancellationToken);

    Task<ApiResponse<JsonElement>> PreviewAsync(ServiceCatalogPreviewCommand command, CancellationToken cancellationToken);

    Task<ApiResponse<JsonElement>> ApplyAsync(ServiceCatalogApplyCommand command, CancellationToken cancellationToken);
}