using BiUM.Core.MessageBroker;
using MemoryPack;

namespace BiApp.Test2.Application.External.Test.Events;

[Event(Exchange = "test")]
[MemoryPackable]
public partial class CurrencyCreatedEvent : BaseEvent
{
    public required string Name { get; set; }
    public required string Code { get; set; }
}

[Event(Exchange = "test")]
[MemoryPackable]
public partial class CurrencyUpdatedEvent : BaseEvent
{
    public required string Name { get; set; }
    public required string Code { get; set; }
}

[Event(Exchange = "test")]
[MemoryPackable]
public partial class CurrencyDeletedEvent : BaseEvent
{
    public required string Name { get; set; }
    public required string Code { get; set; }
}
