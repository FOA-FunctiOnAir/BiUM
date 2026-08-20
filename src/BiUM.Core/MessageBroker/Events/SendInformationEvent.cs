using MemoryPack;
using System;
using System.Collections.Generic;

namespace BiUM.Core.MessageBroker.Events;

[Event(Exchange = "information")]
[MemoryPackable]
public partial class SendInformationEvent : BaseEvent
{
    public Guid? ApplicationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = [];
}