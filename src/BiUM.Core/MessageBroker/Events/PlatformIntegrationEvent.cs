using MemoryPack;
using System;
using System.Collections.Generic;

namespace BiUM.Core.MessageBroker.Events;

[Event]
[MemoryPackable]
public partial class PlatformIntegrationEvent : BaseEvent
{
    public Guid EventId { get; set; }

    public string? EventCode { get; set; }

    public Dictionary<string, string?> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}