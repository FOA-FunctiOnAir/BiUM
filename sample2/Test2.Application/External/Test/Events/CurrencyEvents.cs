using BiUM.Core.MessageBroker;

namespace BiUM.Test2.Application.External.Test.Events;

[Event(Exchange = "test")]
public class CurrencyCreatedEvent : BaseEvent
{
    public string Name { get; set; }
    public string Code { get; set; }
}

[Event(Exchange = "test")]
public class CurrencyUpdatedEvent : BaseEvent
{
    public string Name { get; set; }
    public string Code { get; set; }
}

[Event(Exchange = "test")]
public class CurrencyDeletedEvent : BaseEvent
{
    public string Name { get; set; }
    public string Code { get; set; }
}
