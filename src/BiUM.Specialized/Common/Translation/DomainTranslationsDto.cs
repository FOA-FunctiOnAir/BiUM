using AutoMapper;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using System.Linq;

namespace BiUM.Specialized.Common.Translation;

public class DomainTranslationsDto : BaseDto, IMapFrom<DomainTranslation>
{
    public Guid ApplicationId { get; set; }
    public string? Code { get; set; }
    public string? Text { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DomainTranslation, DomainTranslationsDto>()
            .ForMember(dto => dto.Text, conf => conf.MapFrom(res => res.DomainTranslationDetails.FirstOrDefault()));
    }
}
