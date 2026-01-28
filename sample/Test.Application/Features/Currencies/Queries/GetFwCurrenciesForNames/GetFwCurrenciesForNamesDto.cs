using AutoMapper;
using BiApp.Test.Domain.Entities;
using BiUM.Specialized.Common.Mapper;
using BiUM.Specialized.Mapping;
using System.Linq;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForNames;

public class GetFwCurrenciesForNamesDto : BaseForValuesDto<Currency>, IMapFrom<Currency>
{
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Currency, GetFwCurrenciesForNamesDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.CurrencyTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}
