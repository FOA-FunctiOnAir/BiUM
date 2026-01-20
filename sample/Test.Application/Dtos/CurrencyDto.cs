using AutoMapper;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Mapping;
using BiUM.Test.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Test.Application.Dtos;

public class CurrencyDto : BaseDto, IMapFrom<Currency>
{
    public Guid Type { get; set; }

    public string Name { get; set; }

    public List<EntityTranslationDto>? NameTr { get; set; }

    public string Code { get; set; }

    public string CoinCode { get; set; }

    public int GroupPart { get; set; }

    public int DecimalPart { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Currency, CurrencyDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.CurrencyTranslations.GetColumnTranslation(nameof(res.Name))))
            .ForMember(dto => dto.NameTr, conf => conf.MapFrom(res => res.CurrencyTranslations.GetColumnTranslations(nameof(res.Name))));
    }
}
