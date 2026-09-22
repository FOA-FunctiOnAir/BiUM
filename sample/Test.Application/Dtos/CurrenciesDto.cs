using AutoMapper;
using BiApp.Test.Domain.Entities;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using static BiUM.Specialized.Mapping.TranslationMapping;

namespace BiApp.Test.Application.Dtos;

public class CurrenciesDto : BaseDto, IMapFrom<Currency>
{
    public Guid Type { get; set; }

    public required string Name { get; set; }

    public required string Code { get; set; }

    public required string CoinCode { get; set; }

    public int GroupPart { get; set; }

    public int DecimalPart { get; set; }

    public decimal FxRate { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Currency, CurrenciesDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(GetColumnTranslationExpr<Currency, CurrencyTranslation>(res => res.CurrencyTranslations, nameof(Currency.Name))));
    }
}