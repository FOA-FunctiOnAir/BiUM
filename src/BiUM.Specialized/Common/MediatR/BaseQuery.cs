using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Utils;
using System;
using System.Collections.Generic;

namespace BiUM.Specialized.Common.MediatR;

public record BaseQuery<TType> : BaseRequestDto<TType>
{
    public string? Id { get; init; }

    public IReadOnlyList<Guid> MultipleIds => field ??= CommaSeparatedValueHelper.ParseAsGuids(Id);

    public string? Q { get; init; }
    public int? PageStart { get; init; }
    public int? PageSize { get; init; }
}

public record BaseQueryDto<TType> : BaseQuery<ApiResponse<TType>>
{
}

public record BasePaginatedQueryDto<TType> : BaseQuery<PaginatedApiResponse<TType>>
{
}

public record BaseForValuesQueryDto<TType> : BaseQuery<ApiResponse<IList<TType>>>
{
}

public record BasePaginatedForValuesQueryDto<TType> : BasePaginatedQueryDto<TType>
{
}
