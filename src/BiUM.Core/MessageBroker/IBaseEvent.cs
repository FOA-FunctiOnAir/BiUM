using BiUM.Core.Models;
using System;

namespace BiUM.Core.MessageBroker;

public interface IBaseEvent
{
    public CorrelationContext CorrelationContext { get; set; }
    public DateOnly Created { get; set; }
    public TimeOnly CreatedTime { get; set; }
}
