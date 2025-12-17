using AutoMapper;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using System.Linq;

namespace BiUM.Specialized.Common.Crud;

public class DomainCrudsDto : BaseDto, IMapFrom<DomainCrud>
{
    public Guid MicroserviceId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? TableName { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DomainCrud, DomainCrudsDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.DomainCrudTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}