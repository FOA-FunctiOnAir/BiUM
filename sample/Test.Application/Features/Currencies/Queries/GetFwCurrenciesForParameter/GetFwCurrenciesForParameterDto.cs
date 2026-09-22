using AutoMapper;
using BiApp.Test.Domain.Entities;
using BiUM.Specialized.Common.Mapper;
using static BiUM.Specialized.Mapping.TranslationMapping;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;

public class GetFwCurrenciesForParameterDto : BaseForValuesDto<Currency>
{
    public string? Code { get; set; }

    public static void Mapping(Profile profile)
    {
        profile.CreateMap<Currency, GetFwCurrenciesForParameterDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(GetColumnTranslationExpr<Currency, CurrencyTranslation>(res => res.CurrencyTranslations, nameof(Currency.Name))));
    }
}