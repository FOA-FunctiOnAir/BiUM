using BiUM.Contract.Models.Api;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public interface IDynamicApiEvents
{
    Task<ApiResponse> PublishAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<ApiResponse> PublishAsync(string eventCode, CancellationToken cancellationToken = default);
}