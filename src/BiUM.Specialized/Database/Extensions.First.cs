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
    public static async Task<TDestination> FirstAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var result = await query.FirstAsync(cancellationToken);

        var item = mapper.Map<TDestination>(result);

        return item;
    }

    public static async Task<TDestination> FirstAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var result = await query.FirstAsync(predicate, cancellationToken);

        var item = mapper.Map<TDestination>(result);

        return item;
    }

    public static async Task<TDestination?> FirstOrDefaultAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var result = await query.FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        var item = mapper.Map<TDestination>(result);

        return item;
    }

    public static async Task<TDestination?> FirstOrDefaultAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var result = await query.FirstOrDefaultAsync(predicate, cancellationToken);

        if (result is null)
        {
            return null;
        }

        var item = mapper.Map<TDestination>(result);

        return item;
    }
}