using BiApp.Test.Application.Dtos;
using BiUM.Specialized.Common.MediatR;
using System.Collections.Generic;

namespace BiApp.Test.Application.Features.Currencies.Commands.SaveCurrency;

public record SaveCurrencyCommand : BaseCommandDto
{
    public required IReadOnlyList<EntityTranslationDto> NameTr { get; set; }

    public required string Code { get; set; }
}
