using BiUM.Core.Models;
using MediatR;
using System;

namespace BiUM.Core.MessageBroker;

public class BaseEvent : IBaseEvent, INotification
{
    public CorrelationContext CorrelationContext { get; set; }
    public DateOnly Created { get; set; }
    public TimeOnly CreatedTime { get; set; }
    public Guid? CreatedBy { get; set; }
}
