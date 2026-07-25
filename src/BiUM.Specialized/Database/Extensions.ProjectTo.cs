using AutoMapper;
using AutoMapper.QueryableExtensions;
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
    public static Task<List<TDestination>> ProjectToListAsync<TDestination>(
        this IQueryable source,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TDestination : class
        => source.ProjectTo<TDestination>(mapper.ConfigurationProvider).AsNoTracking().ToListAsync(cancellationToken);

    public static Task<TDestination?> ProjectToFirstOrDefaultAsync<TDestination>(
        this IQueryable source,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TDestination : class
        => source.ProjectTo<TDestination>(mapper.ConfigurationProvider).AsNoTracking().FirstOrDefaultAsync(cancellationToken);

    public static Task<TDestination?> ProjectToFirstOrDefaultAsync<TDestination>(
        this IQueryable source,
        Expression<Func<TDestination, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TDestination : class
        => source.ProjectTo<TDestination>(mapper.ConfigurationProvider).AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

    public static Task<List<TDestination>> ProjectToListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
        => queryable.AsNoTracking()
            .ProjectTo<TDestination>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public static async Task<IList<TDestination>> ProjectToIListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
        => await queryable.ProjectToListAsync<TSource, TDestination>(mapper, cancellationToken);

    public static Task<TDestination?> ProjectToFirstOrDefaultAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
        => queryable.AsNoTracking()
            .ProjectTo<TDestination>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

    public static Task<TDestination?> ProjectToFirstOrDefaultAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TDestination, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
        => queryable.AsNoTracking()
            .ProjectTo<TDestination>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(predicate, cancellationToken);

    public static async Task<PaginatedApiResponse<TDestination>> ProjectToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IBaseQuery baseQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking().ProjectTo<TDestination>(mapper.ConfigurationProvider);

        var items = await query.OrderPaginatedQuery(baseQuery).ToListAsync(cancellationToken);

        return new PaginatedApiResponse<TDestination>(
            baseQuery: baseQuery,
            items: items,
            count: await query.CountAsync(cancellationToken)
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> ProjectToWherePaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IBaseQuery baseQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking().Where(predicate).ProjectTo<TDestination>(mapper.ConfigurationProvider);

        var items = await query.OrderPaginatedQuery(baseQuery).ToListAsync(cancellationToken);

        return new PaginatedApiResponse<TDestination>(
            baseQuery: baseQuery,
            items: items,
            count: await query.CountAsync(cancellationToken)
        );
    }

    public static Task ProjectToMergeSelectedIdsAsync<TSource, TDestination>(
        this PaginatedApiResponse<TDestination> response,
        IReadOnlyList<Guid>? selectedIds,
        IQueryable<TSource> sourceQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
    {
        return response.MergeSelectedIdsAsync(
            selectedIds,
            (missingIds, ct) => sourceQuery
                .Where(x => missingIds.Contains(x.Id))
                .ProjectToListAsync<TSource, TDestination>(mapper, ct),
            cancellationToken);
    }
}