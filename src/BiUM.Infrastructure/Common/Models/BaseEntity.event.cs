using AutoMapper;
using BiUM.Core.MessageBroker;

namespace BiUM.Infrastructure.Common.Models;

public partial class BaseEntity
{
    protected IBaseEvent? CreateEvent<TEvent>(IMapper? mapper)
        where TEvent : IBaseEvent
    {
        return mapper is null ? null : mapper.Map<TEvent>(this);
    }

    public virtual IBaseEvent? AddCreatedEvent(IMapper? mapper, IBaseEvent? baseEvent)
    {
        return baseEvent;
    }

    public virtual IBaseEvent? AddUpdatedEvent(IMapper? mapper, IBaseEvent? baseEvent)
    {
        return baseEvent;
    }

    public virtual IBaseEvent? AddDeletedEvent(IMapper? mapper, IBaseEvent? baseEvent)
    {
        return baseEvent;
    }
}