using AutoMapper;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Specialized.Common.DynamicApi;

public class DomainDynamicApiDto : BaseDto, IMapFrom<DomainDynamicApi>
{
    public Guid ApplicationId { get; set; }
    public Guid MicroserviceId { get; set; }
    public string? Name { get; set; }
    public List<BaseEntityTranslationDto>? NameTr { get; set; }
    public string? Code { get; set; }
    public Guid HttpType { get; set; }
    public Guid ExecutionType { get; set; }
    public Guid RuntimePlatformType { get; set; }
    public string? SourceCode { get; set; }
    public Guid? CompileStatusType { get; set; }
    public string? CompileError { get; set; }
    public bool Compensatible { get; set; }
    public ICollection<DomainDynamicApiParameterDto> DynamicApiParameters { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DomainDynamicApi, DomainDynamicApiDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.DomainDynamicApiTranslations.GetColumnTranslation(nameof(res.Name))))
            .ForMember(dto => dto.NameTr, conf => conf.MapFrom(res => res.DomainDynamicApiTranslations.GetColumnTranslations(nameof(res.Name))));
    }
}

public class DomainDynamicApiParameterDto
{
    public Guid Id { get; set; }
    public Guid DynamicApiId { get; set; }
    public Guid DirectionType { get; set; }
    public required string Property { get; set; }
    public Guid FieldId { get; set; }
    public int _rowStatus { get; set; }
}

public class DomainDynamicApisDto : BaseDto, IMapFrom<DomainDynamicApi>
{
    public Guid ApplicationId { get; set; }
    public Guid MicroserviceId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public Guid HttpType { get; set; }
    public bool Compensatible { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DomainDynamicApi, DomainDynamicApisDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.DomainDynamicApiTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}