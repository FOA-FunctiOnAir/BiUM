using BiUM.Core.MessageBroker;

namespace BiUM.Test2.Application.External.Test.Events;

[Event("test")]
public class CurrencyCreatedEvent : BaseEvent
{
    public string Name { get; set; }

    public string Code { get; set; }
}

[Event("test")]
public class CurrencyUpdatedEvent : BaseEvent
{
    public string Name { get; set; }

    public string Code { get; set; }
}

[Event("test")]
public class CurrencyDeletedEvent : BaseEvent
{
    public string Name { get; set; }

    public string Code { get; set; }
}
