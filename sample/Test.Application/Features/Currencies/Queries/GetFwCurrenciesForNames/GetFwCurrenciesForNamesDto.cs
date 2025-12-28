using AutoMapper;
using BiUM.Specialized.Common.Mapper;
using BiUM.Test.Domain.Entities;
using System.Linq;

namespace BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForNames;

public class GetFwCurrenciesForNamesDto : BaseForValuesDto<Currency>
{
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Currency, GetFwCurrenciesForNamesDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.CurrencyTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}
