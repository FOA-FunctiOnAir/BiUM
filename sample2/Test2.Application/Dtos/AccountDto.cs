using AutoMapper;
using BiApp.Test2.Domain.Entities;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BiApp.Test2.Application.Dtos;

public class AccountDto : BaseDto, IMapFrom<Account>
{
    public Guid Type { get; set; }

    public required string Name { get; set; }

    public List<EntityTranslationDto>? NameTr { get; set; }

    public required string Code { get; set; }

    public required string CoinCode { get; set; }

    public int GroupPart { get; set; }

    public int DecimalPart { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Account, AccountDto>()
            .ForMember(dto => dto.Name, conf => conf.MapFrom(res => res.AccountTranslations.GetColumnTranslation(nameof(res.Name))))
            .ForMember(dto => dto.NameTr, conf => conf.MapFrom(res => res.AccountTranslations.GetColumnTranslations(nameof(res.Name))));
    }
}
