using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.MediatR;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using System.Collections.Generic;

namespace BiUM.Specialized.Common.DynamicApi;

public record SaveDomainDynamicApiCommand : BaseCommandDto
{
    public Guid ApplicationId { get; set; }
    public Guid MicroserviceId { get; set; }
    public IReadOnlyList<BaseEntityTranslationDto>? NameTr { get; set; }
    public required string Code { get; set; }
    public Guid HttpType { get; set; }
    public Guid ExecutionType { get; set; }
    public Guid RuntimePlatformType { get; set; }
    public required string SourceCode { get; set; }
    public bool Compensatible { get; set; }
    public IList<SaveDomainDynamicApiCommandParameter> DynamicApiParameters { get; set; } = [];
}

public class SaveDomainDynamicApiCommandParameter
{
    public Guid Id { get; set; }
    public Guid DirectionType { get; set; }
    public required string Property { get; set; }
    public Guid FieldId { get; set; }
    public int _rowStatus { get; set; }
}

public record PublishDomainDynamicApiCommand : BaseCommandDto;

public record DeleteDomainDynamicApiCommand : BaseCommandDto;

public record SaveExportRequestCommand
{
    public Guid? ApplicationId { get; set; }
    public required string Name { get; set; }
    public Guid? SourceMicroserviceId { get; set; }
    public required string SourceUrl { get; set; }
    public Dictionary<string, object?>? SourceParameters { get; set; }
    public string Format { get; set; } = "xlsx";
}

public record DeleteExportRequestCommand
{
    public Guid Id { get; set; }
}

public record GetExportRequestsQuery
{
    public int? PageStart { get; set; }
    public int? PageSize { get; set; }
}

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