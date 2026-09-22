using AutoMapper;
using BiApp.Test.Domain.Entities;
using BiUM.Specialized.Common.Mapper;
using static BiUM.Specialized.Mapping.TranslationMapping;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForNames;

public class GetFwCurrenciesForNamesDto : BaseForValuesDto<Currency>
{
    public static void Mapping(Profile profile)
    {
        profile.CreateMap<Currency, GetFwCurrenciesForNamesDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(GetColumnTranslationExpr<Currency, CurrencyTranslation>(res => res.CurrencyTranslations, nameof(Currency.Name))));
    }
}