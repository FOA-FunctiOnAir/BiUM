using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.DynamicApi;
using BiUM.Specialized.Services.DynamicExporter;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

[ApiController]
[BiUMBaseRoute]
public class DynamicExporterController : ApiControllerBase
{
    private readonly IDynamicExporterService _dynamicExporterService;

    public DynamicExporterController(IDynamicExporterService dynamicExporterService)
    {
        _dynamicExporterService = dynamicExporterService;
    }

    [HttpPost]
    public Task<ApiResponse<DynamicExportRequestDto>> SaveExportRequest([FromBody] SaveExportRequestCommand command, CancellationToken cancellationToken)
    {
        return _dynamicExporterService.SaveExportRequestAsync(command, cancellationToken);
    }

    [HttpGet]
    public Task<ApiResponse<DynamicExportRequestDto>> GetExportRequest(string id, CancellationToken cancellationToken)
    {
        Guid.TryParse(id, out var guidId);
        return _dynamicExporterService.GetExportRequestAsync(guidId, cancellationToken);
    }

    [HttpGet]
    public Task<PaginatedApiResponse<DynamicExportRequestDto>> GetExportRequests([FromQuery] GetExportRequestsQuery query, CancellationToken cancellationToken)
    {
        return _dynamicExporterService.GetExportRequestsAsync(query, cancellationToken);
    }

    [HttpDelete]
    public Task<ApiResponse> DeleteExportRequest([FromBody] DeleteExportRequestCommand command, CancellationToken cancellationToken)
    {
        return _dynamicExporterService.DeleteExportRequestAsync(command.Id, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Download(string id, CancellationToken cancellationToken)
    {
        Guid.TryParse(id, out var guidId);
        var file = await _dynamicExporterService.DownloadAsync(guidId, cancellationToken);

        if (file is null)
        {
            return NotFound();
        }

        return File(file.Value.Content, file.Value.MimeType, file.Value.FileName);
    }
}