using BiApp.Test2.Domain.Entities;
using BiUM.Core.MessageBroker;
using BiUM.Specialized.Mapping;
using MemoryPack;

namespace BiApp.Test2.Domain.Events;

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
