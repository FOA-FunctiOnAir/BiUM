#nullable enable
using AutoMapper;
using BiApp.Test.Domain.Events;
using BiUM.Core.MessageBroker;

namespace BiApp.Test.Domain.Entities;

public partial class Currency
{
    public override IBaseEvent? AddCreatedEvent(IMapper? mapper, IBaseEvent? baseEvent)
        => CreateEvent<CurrencyCreatedEvent>(mapper);
    public override IBaseEvent? AddUpdatedEvent(IMapper? mapper, IBaseEvent? baseEvent)
        => CreateEvent<CurrencyCreatedEvent>(mapper);
    public override IBaseEvent? AddDeletedEvent(IMapper? mapper, IBaseEvent? baseEvent = null)
        => CreateEvent<CurrencyCreatedEvent>(mapper);
}
