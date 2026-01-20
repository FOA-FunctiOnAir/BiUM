using AutoMapper;
using BiApp.Test2.Domain.Entities;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using System.Linq;

namespace BiApp.Test2.Application.Dtos;

public class AccountsDto : BaseDto, IMapFrom<Account>
{
    public Guid Type { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }

    public string CoinCode { get; set; }

    public int GroupPart { get; set; }

    public int DecimalPart { get; set; }

    public decimal FxRate { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Account, AccountsDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.AccountTranslations.GetColumnTranslation(nameof(res.Name))));
    }
}
