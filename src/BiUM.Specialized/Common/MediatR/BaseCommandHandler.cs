using BiUM.Specialized.Common.API;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.MediatR;

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, ApiEmptyResponse>, IBaseRequestHandler<TCommand>
    where TCommand : IRequest<ApiEmptyResponse>
{
    new Task<ApiEmptyResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandResponseHandler<TCommand, TType> : IRequestHandler<TCommand, ApiResponse<TType>>, IBaseRequestHandler<TCommand, TType>
    where TCommand : IRequest<ApiResponse<TType>>
{
    new Task<ApiResponse<TType>> Handle(TCommand command, CancellationToken cancellationToken);
}