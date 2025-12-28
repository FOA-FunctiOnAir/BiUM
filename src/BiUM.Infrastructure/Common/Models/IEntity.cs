using System;

namespace BiUM.Infrastructure.Common.Models;

public interface IEntity
{
    public Guid Id { get; set; }

    public Guid CorrelationId { get; set; }
}
