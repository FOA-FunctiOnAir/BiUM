using MemoryPack;
using System;

namespace BiUM.Core.MessageBroker.Events;

[MemoryPackable]
[Event(Exchange = "compensation")]
public partial class CompensationSessionFinalizedEvent : BaseEvent
{
    public required Guid CompensationSessionId { get; set; }

    public required bool Success { get; set; }
}