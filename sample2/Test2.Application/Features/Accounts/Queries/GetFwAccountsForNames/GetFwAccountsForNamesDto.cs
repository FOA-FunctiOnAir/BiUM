using AutoMapper;
using BiApp.Test2.Domain.Entities;
using BiUM.Specialized.Common.Mapper;
using System.Linq;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForNames;

public class GetFwAccountsForNamesDto : BaseForValuesDto<Account>
{
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Account, GetFwAccountsForNamesDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.AccountTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}
