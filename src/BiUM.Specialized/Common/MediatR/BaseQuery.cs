using BiUM.Specialized.Common.API;

namespace BiUM.Specialized.Common.MediatR;

public record BaseQuery<TType> : BaseRequestDto<TType>
{
    public Guid? Id { get; set; }
    public IReadOnlyList<Guid>? Ids { get; set; }
    public string? Q { get; set; }
    public int? PageStart { get; set; }
    public int? PageSize { get; set; }
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