using AutoMapper;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using static BiUM.Specialized.Mapping.TranslationMapping;

namespace BiUM.Specialized.Common.Crud;

public class DomainCrudsDto : BaseDto, IMapFrom<DomainCrud>
{
    public Guid ApplicationId { get; set; }
    public Guid MicroserviceId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? TableName { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DomainCrud, DomainCrudsDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(GetColumnTranslationExpr<DomainCrud, DomainCrudTranslation>(res => res.DomainCrudTranslations, nameof(DomainCrud.Name))));
    }
}