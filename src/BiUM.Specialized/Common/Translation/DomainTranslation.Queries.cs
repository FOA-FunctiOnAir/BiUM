using BiUM.Specialized.Common.MediatR;
using System;

namespace BiUM.Specialized.Common.Translation;

public record GetDomainTranslationQuery : BaseQueryDto<DomainTranslationsDto>
{
    public Guid MicroserviceId { get; set; }
}

public record GetDomainTranslationsQuery : BasePaginatedQueryDto<DomainTranslationsDto>
{
    public Guid MicroserviceId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
}