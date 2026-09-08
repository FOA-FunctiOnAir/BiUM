using MediatR;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.MediatR;

public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ActivitySource _activitySource;

    public TracingBehavior(ActivitySource activitySource)
    {
        _activitySource = activitySource;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity(typeof(TRequest).Name, ActivityKind.Internal);

        return await next();
    }
}