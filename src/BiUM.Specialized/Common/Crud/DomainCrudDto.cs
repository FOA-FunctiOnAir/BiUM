using AutoMapper;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using System.Collections.Generic;
using static BiUM.Specialized.Mapping.TranslationMapping;

namespace BiUM.Specialized.Common.Crud;

public class DomainCrudDto : BaseDto, IMapFrom<DomainCrud>
{
    public Guid ApplicationId { get; set; }
    public Guid MicroserviceId { get; set; }
    public string? Name { get; set; }
    public List<BaseEntityTranslationDto>? NameTr { get; set; }
    public string? Code { get; set; }
    public string? TableName { get; set; }
    public bool Compensatible { get; set; }

    public ICollection<DomainCrudColumnDto> DomainCrudColumns { get; set; } = [];

    public ICollection<DomainCrudPartialUpdateDto> DomainCrudPartialUpdates { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DomainCrud, DomainCrudDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(GetColumnTranslationExpr<DomainCrud, DomainCrudTranslation>(res => res.DomainCrudTranslations, nameof(DomainCrud.Name))))
            .ForMember(dto => dto.NameTr, conf => conf.MapFrom(GetColumnTranslationsExpr<DomainCrud, DomainCrudTranslation>(res => res.DomainCrudTranslations, nameof(DomainCrud.Name))));
    }
}