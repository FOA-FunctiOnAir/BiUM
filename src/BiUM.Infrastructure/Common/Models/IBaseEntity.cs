using AutoMapper;
using BiUM.Core.MessageBroker;
using System;

namespace BiUM.Infrastructure.Common.Models;

public interface IBaseEntity : IEntity
{
    public bool Active { get; set; }

    public bool Deleted { get; set; }

    public DateOnly Created { get; set; }

    public TimeOnly CreatedTime { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateOnly? Updated { get; set; }

    public TimeOnly? UpdatedTime { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool Test { get; set; }

    public IBaseEvent? AddCreatedEvent(IMapper? mapper, IBaseEvent? baseEvent);

    public IBaseEvent? AddUpdatedEvent(IMapper? mapper, IBaseEvent? baseEvent);

    public IBaseEvent? AddDeletedEvent(IMapper? mapper, IBaseEvent? baseEvent);
}