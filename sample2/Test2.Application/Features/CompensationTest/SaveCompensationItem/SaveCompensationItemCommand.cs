using BiUM.Specialized.Common.MediatR;

namespace BiApp.Test2.Application.Features.CompensationTest.SaveCompensationItem;

public record SaveCompensationItemCommand : BaseCommandDto
{
    public required string Name { get; set; }
}