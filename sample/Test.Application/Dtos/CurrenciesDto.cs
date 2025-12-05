using AutoMapper;
using BiUM.Test.Domain.Entities;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;

namespace BiUM.Test.Application.Dtos;

public class CurrenciesDto : BaseDto, IMapFrom<Currency>
{
    public Guid Type { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }

    public string CoinCode { get; set; }

    public int GroupPart { get; set; }

    public int DecimalPart { get; set; }

    public decimal FxRate { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Currency, CurrenciesDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.CurrencyTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}