using AutoMapper;
using BiUM.Contract.Models.Api;
using BiUM.Contract.Models.MessageBroker;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    public static async Task<PaginatedApiResponse<TDestination>> ToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        IBaseQuery baseQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
    {
        var result = await sourceQuery
            .ApplyExcludedIds(excludedIds)
            .ToPaginatedListAsync<TSource, TDestination>(baseQuery, mapper, cancellationToken);

        await result.MergeSelectedIdsAsync(selectedIds, sourceQuery, mapper, cancellationToken);

        return result;
    }

    public static Task<PaginatedApiResponse<TDestination>> ToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        int? pageStart,
        int? pageSize,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
        => sourceQuery.ToForParameterPaginatedListAsync<TSource, TDestination>(
            selectedIds,
            excludedIds,
            PaginationQuery.ToPageBaseQuery(pageStart, pageSize),
            mapper,
            cancellationToken);

    public static async Task<PaginatedApiResponse<TDestination>> ProjectToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        IBaseQuery baseQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
    {
        var result = await sourceQuery
            .ApplyExcludedIds(excludedIds)
            .ProjectToPaginatedListAsync<TSource, TDestination>(baseQuery, mapper, cancellationToken);

        await result.ProjectToMergeSelectedIdsAsync(selectedIds, sourceQuery, mapper, cancellationToken);

        return result;
    }

    public static Task<PaginatedApiResponse<TDestination>> ProjectToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        int? pageStart,
        int? pageSize,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
        => sourceQuery.ProjectToForParameterPaginatedListAsync<TSource, TDestination>(
            selectedIds,
            excludedIds,
            PaginationQuery.ToPageBaseQuery(pageStart, pageSize),
            mapper,
            cancellationToken);
}