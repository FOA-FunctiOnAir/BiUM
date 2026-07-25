using BiUM.Infrastructure.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    public static IQueryable<TSource> ApplyIncludedIds<TSource>(
        this IQueryable<TSource> queryable,
        IReadOnlyList<Guid>? includedIds)
        where TSource : class, IEntity
        => queryable.ApplyIncludedIds(includedIds, x => x.Id);

    public static IQueryable<TSource> ApplyIncludedIds<TSource>(
        this IQueryable<TSource> queryable,
        IReadOnlyList<Guid>? includedIds,
        Expression<Func<TSource, Guid>> idSelector)
        where TSource : class
    {
        if (includedIds is not { Count: > 0 })
        {
            return queryable;
        }

        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(Guid)],
            Expression.Constant(includedIds),
            idSelector.Body);

        var lambda = Expression.Lambda<Func<TSource, bool>>(contains, idSelector.Parameters);

        return queryable.Where(lambda);
    }

    public static IEnumerable<T> ApplyIncludedIds<T>(
        this IEnumerable<T> source,
        IReadOnlyList<Guid>? includedIds,
        Func<T, Guid> getId)
    {
        if (includedIds is not { Count: > 0 })
        {
            return source;
        }

        var includedSet = includedIds as HashSet<Guid> ?? includedIds.ToHashSet();

        return source.Where(x => includedSet.Contains(getId(x)));
    }
}