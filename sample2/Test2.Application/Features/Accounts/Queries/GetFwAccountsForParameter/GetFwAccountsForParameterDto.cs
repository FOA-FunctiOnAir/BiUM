using AutoMapper;
using BiApp.Test2.Domain.Entities;
using BiUM.Specialized.Common.Mapper;
using static BiUM.Specialized.Mapping.TranslationMapping;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForParameter;

public class GetFwAccountsForParameterDto : BaseForValuesDto<Account>
{
    public string? Code { get; set; }

    public static void Mapping(Profile profile)
    {
        profile.CreateMap<Account, GetFwAccountsForParameterDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(GetColumnTranslationExpr<Account, AccountTranslation>(res => res.AccountTranslations, nameof(Account.Name))));
    }
}