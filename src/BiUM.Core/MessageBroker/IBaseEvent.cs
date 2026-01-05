using BiUM.Core.Models;
using System;

namespace BiUM.Core.MessageBroker;

public interface IBaseEvent
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public DateOnly Created { get; set; }
    public TimeOnly CreatedTime { get; set; }
    public Guid? CreatedBy { get; set; }
    public CorrelationContext CorrelationContext { get; set; }
}
