using System;
using System.Collections.Generic;

namespace BiUM.Core.PlatformIntegration;

public sealed class PlatformActionRequest
{
    public Guid ActionType { get; init; }

    public Guid? ServiceId { get; init; }

    public Guid? EventId { get; init; }

    public string? EventCode { get; init; }

    public Dictionary<string, object?> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}