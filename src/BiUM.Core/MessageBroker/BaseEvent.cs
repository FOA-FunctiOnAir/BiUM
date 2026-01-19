using MediatR;
using System;

namespace BiUM.Core.MessageBroker;

public class BaseEvent : IBaseEvent, INotification
{
    public Guid Id { get; set; }
    public bool Active { get; set; }
    public bool Deleted { get; set; }
    public Guid CorrelationId { get; set; }
    public DateOnly Created { get; set; }
    public TimeOnly CreatedTime { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateOnly? Updated { get; set; }
    public TimeOnly? UpdatedTime { get; set; }
    public Guid? UpdatedBy { get; set; }
}
