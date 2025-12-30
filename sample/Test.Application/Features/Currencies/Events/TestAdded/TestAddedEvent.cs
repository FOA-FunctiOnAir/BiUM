using BiUM.Core.MessageBroker;

namespace BiUM.Test.Application.Features.Currencies.Events.TestAdded;


[Event]
public class TestAddedEvent : BaseEvent
{
    public string Key { get; set; }
}
