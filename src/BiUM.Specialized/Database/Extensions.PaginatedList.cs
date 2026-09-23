using AutoMapper;
using BiUM.Contract.Models.Api;
using BiUM.Contract.Models.MessageBroker;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public static class PaginationQuery
{
    public static IBaseQuery ToPageBaseQuery(int? pageStart = 0, int? pageSize = 10, string? sortBy = null, SortDirection? sortDirection = null) =>
        new PageOnlyBaseQuery { PageStart = pageStart, PageSize = pageSize, SortBy = sortBy, SortDirection = sortDirection };

    private sealed class PageOnlyBaseQuery : IBaseQuery
    {
        public string? Q { get; init; }

        public Dictionary<string, string>? Filters { get; init; }

        public string? SortBy { get; init; }

        public SortDirection? SortDirection { get; init; }

        public int? PageStart { get; init; }

        public int? PageSize { get; init; }
    }
}

public static partial class Extensions
{
    public static async Task<PaginatedApiResponse<TSource>> ToPaginatedListAsync<TSource>(
        this IQueryable<TSource> queryable,
        IBaseQuery baseQuery,
        CancellationToken cancellationToken = default
    )
        where TSource : class
    {
        var query = queryable.AsNoTracking();

        var items = await query.OrderPaginatedQuery(baseQuery).ToListAsync(cancellationToken);

        return new PaginatedApiResponse<TSource>(
            baseQuery: baseQuery,
            items: items,
            count: await query.CountAsync(cancellationToken)
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> ToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IBaseQuery baseQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            query.OrderPaginatedQuery(baseQuery),
            mapper);

        var items = await projected.ToListAsync(cancellationToken);

        return new PaginatedApiResponse<TDestination>(
            baseQuery: baseQuery,
            items: items,
            count: await query.CountAsync(cancellationToken)
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> ToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IBaseQuery baseQuery,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            query.OrderPaginatedQuery(baseQuery),
            mapper,
            languageId);

        var items = await projected.ToListAsync(cancellationToken);

        return new PaginatedApiResponse<TDestination>(
            baseQuery: baseQuery,
            items: items,
            count: await query.CountAsync(cancellationToken)
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> WhereToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IBaseQuery baseQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking().Where(predicate);

        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            query.OrderPaginatedQuery(baseQuery),
            mapper);

        var items = await projected.ToListAsync(cancellationToken);

        return new PaginatedApiResponse<TDestination>(
            baseQuery: baseQuery,
            items: items,
            count: await query.CountAsync(cancellationToken)
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> WhereToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IBaseQuery baseQuery,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking().Where(predicate);

        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            query.OrderPaginatedQuery(baseQuery),
            mapper,
            languageId);

        var items = await projected.ToListAsync(cancellationToken);

        return new PaginatedApiResponse<TDestination>(
            baseQuery: baseQuery,
            items: items,
            count: await query.CountAsync(cancellationToken)
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> WhereToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, int, bool>> predicate,
        IBaseQuery baseQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking().Where(predicate);

        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            query.OrderPaginatedQuery(baseQuery),
            mapper);

        var items = await projected.ToListAsync(cancellationToken);

        return new PaginatedApiResponse<TDestination>(
            baseQuery: baseQuery,
            items: items,
            count: await query.CountAsync(cancellationToken)
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> WhereToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, int, bool>> predicate,
        IBaseQuery baseQuery,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking().Where(predicate);

        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            query.OrderPaginatedQuery(baseQuery),
            mapper,
            languageId);

        var items = await projected.ToListAsync(cancellationToken);

        return new PaginatedApiResponse<TDestination>(
            baseQuery: baseQuery,
            items: items,
            count: await query.CountAsync(cancellationToken)
        );
    }
}