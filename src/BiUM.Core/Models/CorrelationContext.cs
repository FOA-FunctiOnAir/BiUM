using MessagePack;
using System;

namespace BiUM.Core.Models;

[MessagePackObject]
public class CorrelationContext
{
    public static CorrelationContext Empty { get; } =
        new()
        {
            CorrelationId = Guid.Empty
        };

    [Key(0)]
    public required Guid CorrelationId { get; init; }

    [Key(1)]
    public string? ConnectionId { get; init; }

    [Key(2)]
    public string? TraceId { get; init; }

    [Key(3)]
    public string? IpAddress { get; init; }

    [Key(4)]
    public Guid? ApplicationId { get; init; }

    [Key(5)]
    public Guid? TenantId { get; init; }

    [Key(6)]
    public Guid? LanguageId { get; init; }

    [Key(7)]
    public Guid? ResourceId { get; init; }

    [Key(8)]
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    [Key(9)]
    public UserContext? User { get; init; }
}
