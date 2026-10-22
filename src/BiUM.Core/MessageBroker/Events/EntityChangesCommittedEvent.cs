using MemoryPack;
using System;
using System.Collections.Generic;

namespace BiUM.Core.MessageBroker.Events;

[Event(Exchange = "aiassistant")]
[MemoryPackable]
public partial class EntityChangesCommittedEvent : BaseEvent
{
    public string Domain { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public List<EntityChangeItem> Changes { get; set; } = [];
}

[MemoryPackable]
public partial class EntityChangeItem
{
    public string Table { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public List<Guid> Ids { get; set; } = [];
}

public static class EntityChangeOperations
{
    public const string Created = "Created";

    public const string Updated = "Updated";

    public const string Deleted = "Deleted";
}