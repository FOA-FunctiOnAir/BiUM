using AutoMapper;
using BiApp.Test.Domain.Entities;
using BiUM.Specialized.Common.Mapper;
using BiUM.Specialized.Mapping;
using System.Linq;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;

public class GetFwCurrenciesForParameterDto : BaseForValuesDto<Currency>, IMapFrom<Currency>
{
    public string? Code { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Currency, GetFwCurrenciesForParameterDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.CurrencyTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}