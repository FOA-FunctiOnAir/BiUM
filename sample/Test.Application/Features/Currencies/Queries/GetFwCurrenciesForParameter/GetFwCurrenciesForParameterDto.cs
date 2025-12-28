using AutoMapper;
using BiUM.Specialized.Common.Mapper;
using BiUM.Test.Domain.Entities;
using System.Linq;

namespace BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;

public class GetFwCurrenciesForParameterDto : BaseForValuesDto<Currency>
{
    public string? Code { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Currency, GetFwCurrenciesForParameterDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.CurrencyTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}
