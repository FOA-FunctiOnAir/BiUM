using MemoryPack;
using System;

namespace BiUM.Contract.Models;

[MemoryPackable]
public sealed partial class CorrelationContext
{
    [MemoryPackIgnore]
    public static CorrelationContext Empty { get; } =
        new()
        {
            CorrelationId = Guid.Empty
        };

    public required Guid CorrelationId { get; init; }
    public string? ConnectionId { get; init; }
    public string? TraceId { get; init; }
    public string? IpAddress { get; init; }
    public string? ClientHost { get; init; }
    public Guid? ApplicationId { get; init; }
    public Guid? TenantId { get; init; }
    public string? TenantName { get; init; }
    public Guid? LanguageId { get; init; }
    public Guid? ResourceId { get; init; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public UserContext? User { get; init; }
}