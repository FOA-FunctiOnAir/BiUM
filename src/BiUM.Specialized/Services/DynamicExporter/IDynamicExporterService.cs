using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.DynamicExporter;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicExporter;

public interface IDynamicExporterService
{
    Task<ApiResponse<DynamicExportRequestDto>> SaveExportRequestAsync(SaveExportRequestCommand command, CancellationToken cancellationToken);

    Task<ApiResponse<DynamicExportRequestDto>> GetExportRequestAsync(GetExportRequestQuery query, CancellationToken cancellationToken);

    Task<PaginatedApiResponse<DynamicExportRequestDto>> GetExportRequestsAsync(GetExportRequestsQuery query, CancellationToken cancellationToken);

    Task<ApiResponse> DeleteExportRequestAsync(DeleteExportRequestCommand command, CancellationToken cancellationToken);

    Task<(byte[] Content, string FileName, string MimeType)?> DownloadAsync(DownloadExportRequestQuery query, CancellationToken cancellationToken);

    Task ProcessPendingExportsAsync(CancellationToken cancellationToken);

    Task ExpireOldExportsAsync(CancellationToken cancellationToken);
}