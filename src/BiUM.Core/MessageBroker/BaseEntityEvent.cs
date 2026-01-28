namespace BiUM.Core.MessageBroker;

public class BaseEntityEvent : BaseEvent, IBaseEntityEvent
{
    public bool Test { get; set; } = false;
}
