using BiUM.Core.MessageBroker;
using BiUM.Specialized.Mapping;
using BiUM.Test2.Domain.Entities;
using MemoryPack;

namespace BiUM.Test2.Domain.Events;

[Event]
[MemoryPackable]
public partial class AccountCreatedEvent : BaseEvent, IMapFrom<Account>
{
    public string Name { get; set; }

    public string Code { get; set; }
}

[Event]
[MemoryPackable]
public partial class AccountUpdatedEvent : BaseEvent, IMapFrom<Account>
{
    public string Name { get; set; }

    public string Code { get; set; }
}

[Event]
[MemoryPackable]
public partial class AccountDeletedEvent : BaseEvent, IMapFrom<Account>
{
    public string Name { get; set; }

    public string Code { get; set; }
}
