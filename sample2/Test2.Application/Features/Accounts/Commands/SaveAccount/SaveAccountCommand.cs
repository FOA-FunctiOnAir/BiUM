using BiApp.Test2.Application.Dtos;
using BiUM.Specialized.Common.MediatR;
using System.Collections.Generic;

namespace BiApp.Test2.Application.Features.Accounts.Commands.SaveAccount;

public record SaveAccountCommand : BaseCommandDto
{
    public required IReadOnlyList<EntityTranslationDto> NameTr { get; set; }

    public required string Code { get; set; }
}
