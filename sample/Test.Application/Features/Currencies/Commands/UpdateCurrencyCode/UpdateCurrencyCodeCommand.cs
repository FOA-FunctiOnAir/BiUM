using BiUM.Specialized.Common.MediatR;

namespace BiApp.Test.Application.Features.Currencies.Commands.UpdateCurrencyCode;

public record UpdateCurrencyCodeCommand : BaseCommandDto
{
    public required string Code { get; set; }
}