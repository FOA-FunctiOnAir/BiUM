using AutoMapper;
using BiApp.Test2.Domain.Entities;
using BiUM.Specialized.Common.Mapper;
using BiUM.Specialized.Mapping;
using System.Linq;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForParameter;

public class GetFwAccountsForParameterDto : BaseForValuesDto<Account>, IMapFrom<Account>
{
    public string? Code { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Account, GetFwAccountsForParameterDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.AccountTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}