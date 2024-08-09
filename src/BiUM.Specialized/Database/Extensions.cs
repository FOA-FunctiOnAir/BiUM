﻿using AutoMapper;
using BiUM.Specialized.Common.API;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Specialized.Database;

public static class Extensions
{
    public static async Task<PaginatedApiResponse<TDestination>> ToPaginatedListAsync<TDestination>(this IQueryable<TDestination> queryable, int? pageStart = 0, int? pageSize = 10, CancellationToken cancellationToken = default) where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var _pageStart = !pageStart.HasValue || pageStart.Value < 0 ? 0 : pageStart.Value;
        var _pageSize = !pageSize.HasValue || pageSize.Value < 0 ? 10 : pageSize.Value;

        return new PaginatedApiResponse<TDestination>(
            items: await query.Skip(_pageStart).Take(_pageSize).ToListAsync(cancellationToken),
            count: await query.CountAsync(cancellationToken),
            pageNumber: (_pageStart == 0 ? 0 : _pageStart / _pageSize) + 1,
            pageSize: _pageSize
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> ToPaginatedListAsync<TSource, TDestination>(this IQueryable<TSource> queryable, IMapper mapper, int? pageStart = 0, int? pageSize = 10, CancellationToken cancellationToken = default)
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
}