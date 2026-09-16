using BiUM.Contract.Models.Api;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public sealed class DynamicApiEvents : IDynamicApiEvents
{
    private readonly IDynamicApiEventPublisher _eventPublisher;

    public DynamicApiEvents(IDynamicApiEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public Task<ApiResponse> PublishAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        _eventPublisher.PublishAsync(eventId, cancellationToken);

    public Task<ApiResponse> PublishAsync(string eventCode, CancellationToken cancellationToken = default) =>
        _eventPublisher.PublishAsync(eventCode, cancellationToken);
}