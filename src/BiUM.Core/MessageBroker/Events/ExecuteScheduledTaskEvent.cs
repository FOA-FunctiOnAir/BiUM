using MemoryPack;

namespace BiUM.Core.MessageBroker.Events;

[Event]
[MemoryPackable]
public partial class ExecuteScheduledTaskEvent : BaseEvent
{
    public required string Target { get; set; }
    public required string Task { get; set; }
}