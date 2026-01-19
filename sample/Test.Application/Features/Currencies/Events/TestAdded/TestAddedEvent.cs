using BiUM.Core.MessageBroker;
using MemoryPack;

namespace BiUM.Test.Application.Features.Currencies.Events.TestAdded;


[Event]
[MemoryPackable]
public partial class TestAddedEvent : BaseEvent
{
    public string Key { get; set; }
}
