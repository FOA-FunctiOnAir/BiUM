using BiUM.Contract.Models.Api;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public sealed class NoOpDynamicApiEventPublisher : IDynamicApiEventPublisher
{
    public Task<ApiResponse> PublishAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ApiResponse());

    public Task<ApiResponse> PublishAsync(string eventCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ApiResponse());
}