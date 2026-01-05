using BiUM.Specialized.Common.MediatR;
using BiUM.Test2.Application.Dtos;
using System.Collections.Generic;

namespace BiUM.Test2.Application.Features.Accounts.Commands.SaveAccount;

public record SaveAccountCommand : BaseCommandDto
{
    public required IReadOnlyList<EntityTranslationDto> NameTr { get; set; }

    public string Code { get; set; }
}
