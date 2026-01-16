using AutoMapper;
using BiUM.Core.MessageBroker;
using BiUM.Test.Domain.Events;

namespace BiUM.Test.Domain.Entities;

public partial class Currency
{
    public override IBaseEvent? AddCreatedEvent(IMapper? mapper, IBaseEvent? baseEvent)
        => CreateEvent<CurrencyCreatedEvent>(mapper);
    public override IBaseEvent? AddUpdatedEvent(IMapper? mapper, IBaseEvent? baseEvent)
        => CreateEvent<CurrencyCreatedEvent>(mapper);
    public override IBaseEvent? AddDeletedEvent(IMapper? mapper, IBaseEvent? baseEvent = null)
        => CreateEvent<CurrencyCreatedEvent>(mapper);
}
