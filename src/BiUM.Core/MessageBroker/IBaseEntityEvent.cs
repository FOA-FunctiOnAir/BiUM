using System;

namespace BiUM.Core.MessageBroker;

public interface IBaseEntityEvent : IBaseEvent
{
    public bool Active { get; set; }

    public bool Deleted { get; set; }

    public DateOnly? Updated { get; set; }

    public TimeOnly? UpdatedTime { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool Test { get; set; }
}
