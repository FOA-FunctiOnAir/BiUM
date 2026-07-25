using BiUM.Infrastructure.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    public static IQueryable<TSource> ApplyForNamesIds<TSource>(
        this IQueryable<TSource> queryable,
        IReadOnlyList<Guid>? ids)
        where TSource : class, IEntity
        => queryable.ApplyForNamesIds(ids, x => x.Id);

    public static IQueryable<TSource> ApplyForNamesIds<TSource>(
        this IQueryable<TSource> queryable,
        IReadOnlyList<Guid>? ids,
        Expression<Func<TSource, Guid>> idSelector)
        where TSource : class
    {
        if (ids is not { Count: > 0 })
        {
            return queryable.Where(_ => false);
        }

        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(Guid)],
            Expression.Constant(ids),
            idSelector.Body);

        var lambda = Expression.Lambda<Func<TSource, bool>>(contains, idSelector.Parameters);

        return queryable.Where(lambda);
    }

    public static IEnumerable<T> ApplyForNamesIds<T>(
        this IEnumerable<T> source,
        IReadOnlyList<Guid>? ids,
        Func<T, Guid> getId)
    {
        if (ids is not { Count: > 0 })
        {
            return [];
        }

        var includedSet = ids as HashSet<Guid> ?? ids.ToHashSet();

        return source.Where(x => includedSet.Contains(getId(x)));
    }
}