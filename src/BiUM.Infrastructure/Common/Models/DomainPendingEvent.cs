using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__PENDING_EVENT", Schema = "dbo")]
public class DomainPendingEvent : BaseEntity
{
    [Column("COMPENSATION_SESSION_ID")]
    public Guid CompensationSessionId { get; set; }

    [Column("EVENT_CLR_TYPE_NAME")]
    public required string EventClrTypeName { get; set; }

    [Column("PAYLOAD")]
    public required byte[] Payload { get; set; }

    [Column("DISPATCHED")]
    public bool Dispatched { get; set; }

    [Column("DISPATCHED_AT")]
    public DateTime? DispatchedAt { get; set; }

    [Column("ATTEMPT_COUNT")]
    public int AttemptCount { get; set; }
}