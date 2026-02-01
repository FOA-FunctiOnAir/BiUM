using MemoryPack;

namespace BiUM.Core.MessageBroker.Events;

[Event(Exchange = "scheduler")]
[MemoryPackable]
public partial class ExecuteScheduledTaskEvent : BaseEvent
{
    public required string Target { get; set; }
    public required string Task { get; set; }
}