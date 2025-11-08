using BiUM.Specialized.Common.MediatR;

namespace BiUM.Specialized.Common.Translation;

public record GetDomainTranslationsQuery : BasePaginatedQueryDto<DomainTranslationsDto>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
}