using System;

namespace BiUM.Core.MessageBroker;

public class TenantBaseEntityEvent : BaseEntityEvent
{
    public Guid TenantId { get; set; }
}
