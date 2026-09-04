using BiUM.Specialized.Common.MediatR;
using System;

namespace BiUM.Specialized.Common.DynamicExporter;

public record GetExportRequestQuery : BaseQueryDto<DynamicExportRequestDto>;

public record GetExportRequestsQuery : BasePaginatedQueryDto<DynamicExportRequestDto>
{
    public Guid? StatusId { get; set; }
}

public record DownloadExportRequestQuery : BaseQueryDto<DynamicExportRequestDto>;