using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public interface IDynamicApiHandler
{
    Task<object> ExecuteAsync(IDynamicApiExecutionContext context, CancellationToken cancellationToken);
}