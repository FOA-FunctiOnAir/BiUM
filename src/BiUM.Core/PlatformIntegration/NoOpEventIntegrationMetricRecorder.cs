using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.PlatformIntegration;

public sealed class NoOpEventIntegrationMetricRecorder : IEventIntegrationMetricRecorder
{
    public Task RecordAsync(
        Guid? eventId,
        string? eventCode,
        Guid statusType,
        int durationMs,
        bool success,
        string? detail,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}