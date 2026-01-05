using System;

namespace BiUM.Core.MessageBroker;

public class BaseEntityEvent : BaseEvent
{
    public bool Active { get; set; } = true;

    public bool Deleted { get; set; } = false;

    public DateOnly? Updated { get; set; }

    public TimeOnly? UpdatedTime { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool Test { get; set; } = false;
}
