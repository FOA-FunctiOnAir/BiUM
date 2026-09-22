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
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking(),
            mapper);

        return await projected.FirstAsync(cancellationToken);
    }

    public static async Task<TDestination> FirstAsync<TSource, TDestination>(
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

        return await projected.FirstAsync(cancellationToken);
    }

    public static async Task<TDestination> FirstAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking().Where(predicate),
            mapper);

        return await projected.FirstAsync(cancellationToken);
    }

    public static async Task<TDestination> FirstAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking().Where(predicate),
            mapper,
            languageId);

        return await projected.FirstAsync(cancellationToken);
    }

    public static async Task<TDestination?> FirstOrDefaultAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking(),
            mapper);

        return await projected.FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<TDestination?> FirstOrDefaultAsync<TSource, TDestination>(
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

        return await projected.FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<TDestination?> FirstOrDefaultAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking().Where(predicate),
            mapper);

        return await projected.FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<TDestination?> FirstOrDefaultAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default)
        where TSource : class
        where TDestination : class
    {
        var projected = CorrelationContextLanguage.ProjectToMapped<TSource, TDestination>(
            queryable.AsNoTracking().Where(predicate),
            mapper,
            languageId);

        return await projected.FirstOrDefaultAsync(cancellationToken);
    }
}