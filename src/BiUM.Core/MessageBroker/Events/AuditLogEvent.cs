using System;

namespace BiUM.Core.MessageBroker.Events;

[Event("audit")]
public class AuditLogEvent : BaseEvent
{
    public string ServiceName { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public string? ChangedFieldsJson { get; set; }

    public int ChangeCount { get; set; }
}
