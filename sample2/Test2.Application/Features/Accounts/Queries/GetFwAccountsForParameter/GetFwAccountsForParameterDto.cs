using AutoMapper;
using BiUM.Specialized.Common.Mapper;
using BiUM.Test2.Domain.Entities;
using System.Linq;

namespace BiUM.Test2.Application.Features.Accounts.Queries.GetFwCurrenciesForParameter;

public class GetFwAccountsForParameterDto : BaseForValuesDto<Account>
{
    public string? Code { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Account, GetFwAccountsForParameterDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.AccountTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}
