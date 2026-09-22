using AutoMapper;
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
    public static async Task<List<TSource>> WhereToListAsync<TSource>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
        where TSource : class
    {
        var query = queryable.AsNoTracking().Where(predicate);

        var items = await query.ToListAsync(cancellationToken);

        return items;
    }

    public static async Task<List<TSource>> WhereToListAsync<TSource>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, int, bool>> predicate,
        CancellationToken cancellationToken = default
    )
        where TSource : class
    {
        var query = queryable.AsNoTracking().Where(predicate);

        var items = await query.ToListAsync(cancellationToken);

        return items;
    }

    public static async Task<List<TDestination>> WhereToListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking().Where(predicate),
            mapper);

        return await projected.ToListAsync(cancellationToken);
    }

    public static async Task<List<TDestination>> WhereToListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking().Where(predicate),
            mapper,
            languageId);

        return await projected.ToListAsync(cancellationToken);
    }

    public static async Task<List<TDestination>> WhereToListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, int, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking().Where(predicate),
            mapper);

        return await projected.ToListAsync(cancellationToken);
    }

    public static async Task<List<TDestination>> WhereToListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, int, bool>> predicate,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking().Where(predicate),
            mapper,
            languageId);

        return await projected.ToListAsync(cancellationToken);
    }
}