using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    public static async Task<TDestination> LastAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var item = mapper.Map<TDestination>(await query.LastAsync(cancellationToken));

        return item;
    }

    public static async Task<TDestination> LastAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var item = mapper.Map<TDestination>(await query.LastAsync(predicate, cancellationToken));

        return item;
    }

    public static async Task<TDestination> LastOrDefaultAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var item = mapper.Map<TDestination>(await query.LastOrDefaultAsync(cancellationToken));

        return item;
    }

    public static async Task<TDestination> LastOrDefaultAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var item = mapper.Map<TDestination>(await query.LastOrDefaultAsync(predicate, cancellationToken));

        return item;
    }
}