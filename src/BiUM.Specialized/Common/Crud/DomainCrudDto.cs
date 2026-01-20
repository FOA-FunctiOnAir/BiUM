using AutoMapper;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Specialized.Common.Crud;

public class DomainCrudDto : BaseDto, IMapFrom<DomainCrud>
{
    public Guid MicroserviceId { get; set; }
    public string? Name { get; set; }
    public List<BaseEntityTranslationDto>? NameTr { get; set; }
    public string? Code { get; set; }
    public string? TableName { get; set; }

    public virtual ICollection<DomainCrudColumnDto>? DomainCrudColumns { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DomainCrud, DomainCrudDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.DomainCrudTranslations.GetColumnTranslation(nameof(res.Name))))
            .ForMember(dto => dto.NameTr, conf => conf.MapFrom(res => res.DomainCrudTranslations.GetColumnTranslations(nameof(res.Name))));
    }
}
