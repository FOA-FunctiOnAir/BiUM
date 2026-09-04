using BiUM.Specialized.Common.MediatR;

namespace BiUM.Specialized.Common.DynamicExporter;

public record GetExportRequestQuery : BaseQueryDto<DynamicExportRequestDto>;

public record GetExportRequestsQuery : BasePaginatedQueryDto<DynamicExportRequestDto>;

public record DownloadExportRequestQuery : BaseQueryDto<DynamicExportRequestDto>;