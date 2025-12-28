using BiUM.Specialized.Common.MediatR;
using System;

namespace BiUM.Specialized.Common.Crud;

public record DeleteCrudCommand : BaseCommandDto
{
    public Guid MicroserviceId { get; set; }
}
