using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.DynamicExporter;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Services.DynamicExporter;
using Microsoft.AspNetCore.Mvc;
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
    public Task<ApiResponse<DynamicExportRequestDto>> GetExportRequest([FromQuery] GetExportRequestQuery query, CancellationToken cancellationToken)
    {
        return _dynamicExporterService.GetExportRequestAsync(query, cancellationToken);
    }

    [HttpGet]
    public Task<PaginatedApiResponse<DynamicExportRequestDto>> GetExportRequests([FromQuery] GetExportRequestsQuery query, CancellationToken cancellationToken)
    {
        return _dynamicExporterService.GetExportRequestsAsync(query, cancellationToken);
    }

    [HttpDelete]
    public Task<ApiResponse> DeleteExportRequest([FromBody] DeleteExportRequestCommand command, CancellationToken cancellationToken)
    {
        return _dynamicExporterService.DeleteExportRequestAsync(command, cancellationToken);
    }

    [HttpGet]
    public Task<ApiResponse<ExportDto>> Download([FromQuery] DownloadExportRequestQuery query, CancellationToken cancellationToken)
    {
        return _dynamicExporterService.DownloadAsync(query, cancellationToken);
    }
}