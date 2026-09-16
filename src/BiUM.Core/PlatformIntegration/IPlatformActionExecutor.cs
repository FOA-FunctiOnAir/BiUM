using BiUM.Contract.Models.Api;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.PlatformIntegration;

public interface IPlatformActionExecutor
{
    Task<ApiResponse> ExecuteAsync(PlatformActionRequest request, CancellationToken cancellationToken = default);
}