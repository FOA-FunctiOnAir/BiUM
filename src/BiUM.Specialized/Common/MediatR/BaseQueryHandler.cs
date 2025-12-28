using BiUM.Specialized.Common.API;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.MediatR;

public interface IQueryHandler<TQuery, TType> : IRequestHandler<TQuery, ApiResponse<TType>>, IBaseRequestHandler<TQuery, TType>
    where TQuery : BaseQueryDto<TType>
{
    new Task<ApiResponse<TType>> Handle(TQuery query, CancellationToken cancellationToken);
}

public interface IPaginatedQueryHandler<TQuery, TType> : IRequestHandler<TQuery, PaginatedApiResponse<TType>>, IBaseRequestHandler<TQuery, TType>, IBaseRequestHandler<TQuery> where TQuery : BasePaginatedQueryDto<TType>
{
    new Task<PaginatedApiResponse<TType>> Handle(TQuery query, CancellationToken cancellationToken);
}

public interface IForValuesQueryHandler<TQuery, TType> : IRequestHandler<TQuery, ApiResponse<IList<TType>>>, IBaseRequestHandler<TQuery, TType>
    where TQuery : BaseForValuesQueryDto<TType>
{
    new Task<ApiResponse<IList<TType>>> Handle(TQuery query, CancellationToken cancellationToken);
}

public interface IPaginatedForValuesQueryHandler<TQuery, TType> : IRequestHandler<TQuery, PaginatedApiResponse<TType>>, IBaseRequestHandler<TQuery, TType>
    where TQuery : BasePaginatedForValuesQueryDto<TType>
{
    new Task<PaginatedApiResponse<TType>> Handle(TQuery query, CancellationToken cancellationToken);
}
