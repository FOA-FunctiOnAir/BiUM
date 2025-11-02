using BiUM.Test.Application.Dtos;
using BiUM.Specialized.Common.MediatR;

namespace BiUM.Test.Application.Features.Currencies.Commands.SaveCurrency;

public record SaveCurrencyCommand : BaseCommandDto
{
    public required IReadOnlyList<EntityTranslationDto> NameTr { get; set; }

    public string Code { get; set; }
}