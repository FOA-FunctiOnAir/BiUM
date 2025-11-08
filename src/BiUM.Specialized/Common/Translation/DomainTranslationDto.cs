using AutoMapper;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;

namespace BiUM.Specialized.Common.Translation;

public class DomainTranslationDto : BaseDto, IMapFrom<DomainTranslation>
{
    public Guid ApplicationId { get; set; }
    public string? Code { get; set; }
    public ICollection<DomainTranslationDetailDto>? DomainTranslationDetails { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DomainTranslation, DomainTranslationDto>()
            .ForMember(dto => dto.DomainTranslationDetails, conf => conf.MapFrom(res => res.DomainTranslationDetails));
    }
}