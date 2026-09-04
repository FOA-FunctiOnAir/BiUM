using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Mapping;
using System;

namespace BiUM.Specialized.Common.DynamicExporter;

public class DynamicExportRequestDto : BaseDto, IMapFrom<DomainDynamicExportRequest>
{
    public Guid ApplicationId { get; set; }
    public Guid? SourceMicroserviceId { get; set; }
    public required string Name { get; set; }
    public Guid Status { get; set; }
    public required string SourceUrl { get; set; }
    public string? SourceParameters { get; set; }
    public required string Format { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public int? RowCount { get; set; }
    public int? SourceTotalCount { get; set; }
    public bool Truncated { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ExpiresAt { get; set; }
}