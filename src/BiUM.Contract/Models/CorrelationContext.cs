using MemoryPack;
using System;

namespace BiUM.Contract.Models;

[MemoryPackable]
public sealed partial class CorrelationContext
{
    public static readonly Guid DefaultLanguageId = Guid.Parse("c7b4e773-c3c7-5923-a931-d3f3d6fc66d3");

    [MemoryPackIgnore]
    public static CorrelationContext Empty { get; } =
        new()
        {
            CorrelationId = Guid.Empty,
            ApplicationId = Guid.Empty,
            LanguageId = DefaultLanguageId,
        };

    public required Guid CorrelationId { get; init; }
    public Guid? CompensationSessionId { get; init; }
    public string? ConnectionId { get; init; }
    public string? TraceId { get; init; }
    public string? IpAddress { get; init; }
    public string? ClientHost { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid? TenantId { get; init; }
    public string? TenantName { get; init; }
    public Guid LanguageId { get; init; }
    public Guid? ResourceId { get; init; }
    public Guid? ClientId { get; init; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public UserContext? User { get; init; }

    public CorrelationContext WithCompensationSessionId(Guid compensationSessionId)
    {
        return new CorrelationContext
        {
            CorrelationId = CorrelationId,
            CompensationSessionId = compensationSessionId,
            ConnectionId = ConnectionId,
            TraceId = TraceId,
            IpAddress = IpAddress,
            ClientHost = ClientHost,
            ApplicationId = ApplicationId,
            TenantId = TenantId,
            TenantName = TenantName,
            LanguageId = LanguageId,
            ResourceId = ResourceId,
            ClientId = ClientId,
            User = User
        };
    }

    // Bir yayıncının (publisher) aktif compensation session'ı, asenkron event tüketicilerine
    // örtük olarak miras kalmamalı — tüketici, yayıncının senkron request ömrüyle senkronize değil,
    // yayıncının session'ı zaten finalize edilmiş olabilir (bkz. CompensationEntityProcessor).
    // Sadece CompensationSessionFinalizedEvent'in kendisi bu id'yi taşımaya devam etmeli.
    public CorrelationContext WithoutCompensationSessionId()
    {
        if (CompensationSessionId is null)
        {
            return this;
        }

        return new CorrelationContext
        {
            CorrelationId = CorrelationId,
            CompensationSessionId = null,
            ConnectionId = ConnectionId,
            TraceId = TraceId,
            IpAddress = IpAddress,
            ClientHost = ClientHost,
            ApplicationId = ApplicationId,
            TenantId = TenantId,
            TenantName = TenantName,
            LanguageId = LanguageId,
            ResourceId = ResourceId,
            ClientId = ClientId,
            User = User
        };
    }
}