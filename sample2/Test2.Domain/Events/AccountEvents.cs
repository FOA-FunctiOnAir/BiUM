using BiUM.Core.MessageBroker;
using BiUM.Specialized.Mapping;
using BiUM.Test2.Domain.Entities;

namespace BiUM.Test2.Domain.Events;

[Event]
public class AccountCreatedEvent : BaseEvent, IMapFrom<Account>
{
    public string Name { get; set; }

    public string Code { get; set; }
}

[Event]
public class AccountUpdatedEvent : BaseEvent, IMapFrom<Account>
{
    public string Name { get; set; }

    public string Code { get; set; }
}

[Event]
public class AccountDeletedEvent : BaseEvent, IMapFrom<Account>
{
    public string Name { get; set; }

    public string Code { get; set; }
}
