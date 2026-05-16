using AutoMapper;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Mapping;
using System;

namespace BiUM.Specialized.Common.Crud;

public class DomainCrudPartialUpdateColumnDto : BaseDto, IMapFrom<DomainCrudPartialUpdateColumn>
{
    public Guid PartialUpdateId { get; set; }
    public Guid CrudColumnId { get; set; }
    public string? PropertyName { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DomainCrudPartialUpdateColumn, DomainCrudPartialUpdateColumnDto>()
            .ForMember(d => d.PropertyName, c => c.MapFrom(s => s.CrudColumn.PropertyName));
    }
}