using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Core.PlatformIntegration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public sealed class DynamicApiEventPublisher : IDynamicApiEventPublisher
{
    private readonly IPlatformActionExecutor _platformActionExecutor;

    public DynamicApiEventPublisher(IPlatformActionExecutor platformActionExecutor)
    {
        _platformActionExecutor = platformActionExecutor;
    }

    public Task<ApiResponse> PublishAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        _platformActionExecutor.ExecuteAsync(
            new PlatformActionRequest
            {
                ActionType = Ids.Parameter.EventActionType.Values.PublishEvent,
                EventId = eventId
            },
            cancellationToken);

    public Task<ApiResponse> PublishAsync(string eventCode, CancellationToken cancellationToken = default) =>
        _platformActionExecutor.ExecuteAsync(
            new PlatformActionRequest
            {
                ActionType = Ids.Parameter.EventActionType.Values.PublishEvent,
                EventCode = eventCode
            },
            cancellationToken);
}