namespace BiUM.Core.MessageBroker;

public interface IBaseEntityEvent : IBaseEvent
{
    public bool Test { get; set; }
}