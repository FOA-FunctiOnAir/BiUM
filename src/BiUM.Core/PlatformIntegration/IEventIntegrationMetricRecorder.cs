using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.PlatformIntegration;

public interface IEventIntegrationMetricRecorder
{
    Task RecordAsync(
        Guid? eventId,
        string? eventCode,
        Guid statusType,
        int durationMs,
        bool success,
        string? detail,
        CancellationToken cancellationToken = default);
}