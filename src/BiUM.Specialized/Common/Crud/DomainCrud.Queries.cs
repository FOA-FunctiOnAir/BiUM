using BiUM.Specialized.Common.MediatR;

namespace BiUM.Specialized.Common.Crud;

public record GetDomainCrudsQuery : BasePaginatedQueryDto<DomainCrudsDto>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
}