using BiUM.Specialized.Common.MediatR;

namespace BiUM.Specialized.Common.Crud;

public record DeleteCrudCommand : BaseCommandDto
{
    public Guid MicroserviceId { get; set; }
}