using BiUM.Core.MessageBroker;
using MemoryPack;

namespace BiApp.Test2.Application.External.Test.Events;

[Event(Exchange = "test")]
[MemoryPackable]
public partial class CurrencyCreatedEvent : BaseEvent
{
    public string Name { get; set; }
    public string Code { get; set; }
}

[Event(Exchange = "test")]
[MemoryPackable]
public partial class CurrencyUpdatedEvent : BaseEvent
{
    public string Name { get; set; }
    public string Code { get; set; }
}

[Event(Exchange = "test")]
[MemoryPackable]
public partial class CurrencyDeletedEvent : BaseEvent
{
    public string Name { get; set; }
    public string Code { get; set; }
}
