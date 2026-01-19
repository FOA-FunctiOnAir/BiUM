using BiUM.Core.MessageBroker;
using BiUM.Specialized.Mapping;
using BiUM.Test.Domain.Entities;
using MemoryPack;

namespace BiUM.Test.Domain.Events;

[Event]
[MemoryPackable]
public partial class CurrencyCreatedEvent : BaseEvent, IMapFrom<Currency>
{
    public string Name { get; set; }

    public string Code { get; set; }
}

[Event]
[MemoryPackable]
public partial class CurrencyUpdatedEvent : BaseEvent, IMapFrom<Currency>
{
    public string Name { get; set; }

    public string Code { get; set; }
}

[Event]
[MemoryPackable]
public partial class CurrencyDeletedEvent : BaseEvent, IMapFrom<Currency>
{
    public string Name { get; set; }

    public string Code { get; set; }
}
