using BiUM.Specialized.Common.MediatR;
using BiUM.Specialized.Common.Models;
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