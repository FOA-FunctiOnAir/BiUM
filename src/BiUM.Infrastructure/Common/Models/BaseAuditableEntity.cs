using System;

namespace BiUM.Infrastructure.Common.Models;

public class BaseAuditableEntity : IEntity
{
    public virtual Guid Id { get; set; }

    public virtual Guid CorrelationId { get; set; }
}