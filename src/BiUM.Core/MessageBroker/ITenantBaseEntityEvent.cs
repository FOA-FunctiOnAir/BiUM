using System;

namespace BiUM.Core.MessageBroker;

public interface ITenantBaseEntityEvent : IBaseEntityEvent
{
    public Guid TenantId { get; set; }
}
