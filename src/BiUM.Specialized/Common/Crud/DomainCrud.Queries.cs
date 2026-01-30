using BiUM.Specialized.Common.MediatR;
using System;

namespace BiUM.Specialized.Common.Crud;

public record GetDomainCrudsQuery : BasePaginatedQueryDto<DomainCrudsDto>
{
    public Guid? ApplicationId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
}