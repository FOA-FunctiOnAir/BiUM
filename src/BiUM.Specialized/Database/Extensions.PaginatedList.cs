using AutoMapper;
using BiUM.Contract.Models.Api;
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
    public static async Task<PaginatedApiResponse<TSource>> ToPaginatedListAsync<TSource>(
        this IQueryable<TSource> queryable,
        int? pageStart = 0,
        int? pageSize = 10,
        CancellationToken cancellationToken = default
    )
        where TSource : class
    {
        var query = queryable.AsNoTracking();

        var _pageStart = !pageStart.HasValue || pageStart.Value < 0 ? 0 : pageStart.Value;
        var _pageSize = !pageSize.HasValue || pageSize.Value < 0 ? 10 : pageSize.Value;

        var items = await query.Skip(_pageStart).Take(_pageSize).ToListAsync(cancellationToken);

        return new PaginatedApiResponse<TSource>(
            items: items,
            count: await query.CountAsync(cancellationToken),
            pageNumber: (_pageStart == 0 ? 0 : _pageStart / _pageSize) + 1,
            pageSize: _pageSize
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> ToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        int? pageStart = 0,
        int? pageSize = 10,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var _pageStart = !pageStart.HasValue || pageStart.Value < 0 ? 0 : pageStart.Value;
        var _pageSize = !pageSize.HasValue || pageSize.Value < 0 ? 10 : pageSize.Value;

        var items = mapper.Map<List<TDestination>>(await query.Skip(_pageStart).Take(_pageSize).ToListAsync(cancellationToken));

        return new PaginatedApiResponse<TDestination>(
            items: items,
            count: await query.CountAsync(cancellationToken),
            pageNumber: (_pageStart == 0 ? 0 : _pageStart / _pageSize) + 1,
            pageSize: _pageSize
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> WhereToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        int? pageStart = 0,
        int? pageSize = 10,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking().Where(predicate);

        var _pageStart = !pageStart.HasValue || pageStart.Value < 0 ? 0 : pageStart.Value;
        var _pageSize = !pageSize.HasValue || pageSize.Value < 0 ? 10 : pageSize.Value;

        var items = mapper.Map<List<TDestination>>(await query.Skip(_pageStart).Take(_pageSize).ToListAsync(cancellationToken));

        return new PaginatedApiResponse<TDestination>(
            items: items,
            count: await query.CountAsync(cancellationToken),
            pageNumber: (_pageStart == 0 ? 0 : _pageStart / _pageSize) + 1,
            pageSize: _pageSize
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> WhereToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, int, bool>> predicate,
        IMapper mapper,
        int? pageStart = 0,
        int? pageSize = 10,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking().Where(predicate);

        var _pageStart = !pageStart.HasValue || pageStart.Value < 0 ? 0 : pageStart.Value;
        var _pageSize = !pageSize.HasValue || pageSize.Value < 0 ? 10 : pageSize.Value;

        var items = mapper.Map<List<TDestination>>(await query.Skip(_pageStart).Take(_pageSize).ToListAsync(cancellationToken));

        return new PaginatedApiResponse<TDestination>(
            items: items,
            count: await query.CountAsync(cancellationToken),
            pageNumber: (_pageStart == 0 ? 0 : _pageStart / _pageSize) + 1,
            pageSize: _pageSize
        );
    }
}