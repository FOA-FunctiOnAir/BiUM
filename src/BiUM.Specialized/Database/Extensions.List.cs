using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    public static async Task<List<TDestination>> ToListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking(),
            mapper);

        return await projected.ToListAsync(cancellationToken);
    }

    public static async Task<List<TDestination>> ToListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking(),
            mapper,
            languageId);

        return await projected.ToListAsync(cancellationToken);
    }

    public static async Task<IList<TDestination>> ToIListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        return await queryable.ToListAsync<TSource, TDestination>(mapper, cancellationToken);
    }

    public static async Task<IList<TDestination>> ToIListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        return await queryable.ToListAsync<TSource, TDestination>(mapper, languageId, cancellationToken);
    }
}