using BiUM.Core.HttpClients;
using BiUM.Core.PlatformIntegration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.PlatformIntegration;

public sealed class HttpEventIntegrationMetricRecorder : IEventIntegrationMetricRecorder
{
    private readonly IHttpClientsService _httpClientsService;

    public HttpEventIntegrationMetricRecorder(IHttpClientsService httpClientsService)
    {
        _httpClientsService = httpClientsService;
    }

    public Task RecordAsync(
        Guid? eventId,
        string? eventCode,
        Guid statusType,
        int durationMs,
        bool success,
        string? detail,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, dynamic>
        {
            ["eventId"] = eventId ?? Guid.Empty,
            ["eventCode"] = eventCode ?? string.Empty,
            ["statusType"] = statusType,
            ["durationMs"] = durationMs,
            ["success"] = success,
            ["detail"] = detail ?? string.Empty
        };

        return _httpClientsService.Post(
            "/api/observability/EventIntegrationMetric/RecordEventIntegrationMetric",
            parameters,
            cancellationToken: cancellationToken);
    }
}