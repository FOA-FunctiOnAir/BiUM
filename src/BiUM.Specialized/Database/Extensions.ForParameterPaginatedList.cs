using AutoMapper;
using BiUM.Contract.Models.Api;
using BiUM.Contract.Models.MessageBroker;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Mapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    public static async Task<PaginatedApiResponse<TDestination>> ToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        IBaseQuery baseQuery,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
    {
        var result = await sourceQuery
            .ApplyExcludedIds(excludedIds)
            .ToPaginatedListAsync<TSource, TDestination>(baseQuery, mapper, languageId, cancellationToken);

        await result.MergeSelectedIdsAsync(selectedIds, sourceQuery, mapper, languageId, cancellationToken);

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

    public static Task<PaginatedApiResponse<TDestination>> ToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        int? pageStart,
        int? pageSize,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
        => sourceQuery.ToForParameterPaginatedListAsync<TSource, TDestination>(
            selectedIds,
            excludedIds,
            PaginationQuery.ToPageBaseQuery(pageStart, pageSize),
            mapper,
            languageId,
            cancellationToken);

    public static Task<PaginatedApiResponse<TDestination>> ProjectToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        IBaseQuery baseQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
        => sourceQuery.ToForParameterPaginatedListAsync<TSource, TDestination>(
            selectedIds,
            excludedIds,
            baseQuery,
            mapper,
            cancellationToken);

    public static Task<PaginatedApiResponse<TDestination>> ProjectToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        IBaseQuery baseQuery,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
        => sourceQuery.ToForParameterPaginatedListAsync<TSource, TDestination>(
            selectedIds,
            excludedIds,
            baseQuery,
            mapper,
            languageId,
            cancellationToken);

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
        => sourceQuery.ToForParameterPaginatedListAsync<TSource, TDestination>(
            selectedIds,
            excludedIds,
            PaginationQuery.ToPageBaseQuery(pageStart, pageSize),
            mapper,
            cancellationToken);

    public static Task<PaginatedApiResponse<TDestination>> ProjectToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        int? pageStart,
        int? pageSize,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
        => sourceQuery.ToForParameterPaginatedListAsync<TSource, TDestination>(
            selectedIds,
            excludedIds,
            PaginationQuery.ToPageBaseQuery(pageStart, pageSize),
            mapper,
            languageId,
            cancellationToken);

    public static async Task<PaginatedApiResponse<TDestination>> SelectToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        Expression<Func<TSource, TDestination>> selector,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        IBaseQuery baseQuery,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
    {
        var filteredQuery = sourceQuery.ApplyExcludedIds(excludedIds);

        var result = await filteredQuery
            .Select(selector)
            .OrderBy(d => d.Name)
            .ToPaginatedListAsync(baseQuery, cancellationToken);

        await result.MergeSelectedIdsAsync(
            selectedIds,
            (missingIds, ct) => EntityFrameworkQueryableExtensions.ToListAsync(
                filteredQuery.Where(x => missingIds.Contains(x.Id)).Select(selector),
                ct),
            cancellationToken);

        return result;
    }

    public static Task<PaginatedApiResponse<TDestination>> SelectToForParameterPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> sourceQuery,
        Expression<Func<TSource, TDestination>> selector,
        IReadOnlyList<Guid>? selectedIds,
        IReadOnlyList<Guid>? excludedIds,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
        => sourceQuery.SelectToForParameterPaginatedListAsync(
            selector,
            selectedIds,
            excludedIds,
            PaginationQuery.ToPageBaseQuery(pageStart, pageSize),
            cancellationToken);
}