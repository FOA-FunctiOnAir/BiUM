using BiUM.Core.MessageBroker;
using MemoryPack;

namespace BiApp.Test.Application.Features.Currencies.Events.TestAdded;


[Event]
[MemoryPackable]
public partial class TestAddedEvent : BaseEvent
{
    public required string Key { get; set; }
}