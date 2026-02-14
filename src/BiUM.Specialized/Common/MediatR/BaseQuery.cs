using BiUM.Contract.Models.Api;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace BiUM.Specialized.Common.MediatR;

public record BaseQuery<TType> : BaseRequestDto<TType>
{
    public Guid? Id { get; init; }
    public IReadOnlyList<Guid>? Ids { get; init; }

    public string? Q { get; init; }
    [FromQuery(Name = "Filters")]
    public Dictionary<string, string>? Filters { get; init; }
    public int? PageStart { get; init; }
    public int? PageSize { get; init; }
}

public record BaseQueryDto<TType> : BaseQuery<ApiResponse<TType>>;

public record BasePaginatedQueryDto<TType> : BaseQuery<PaginatedApiResponse<TType>>;

public record BaseForValuesQueryDto<TType> : BaseQuery<ApiResponse<IList<TType>>>;

public record BasePaginatedForValuesQueryDto<TType> : BasePaginatedQueryDto<TType>;