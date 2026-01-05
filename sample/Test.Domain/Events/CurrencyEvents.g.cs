using BiUM.Core.MessageBroker;
using BiUM.Specialized.Mapping;
using BiUM.Test.Domain.Entities;

namespace BiUM.Test.Domain.Events;

[Event]
public class CurrencyCreatedEvent : BaseEvent, IMapFrom<Currency>
{
    public string Name { get; set; }

    public string Code { get; set; }
}

[Event]
public class CurrencyUpdatedEvent : BaseEvent, IMapFrom<Currency>
{
    public string Name { get; set; }

    public string Code { get; set; }
}

[Event]
public class CurrencyDeletedEvent : BaseEvent, IMapFrom<Currency>
{
    public string Name { get; set; }

    public string Code { get; set; }
}
