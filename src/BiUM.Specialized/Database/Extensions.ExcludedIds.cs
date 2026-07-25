using BiUM.Infrastructure.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    public static IQueryable<TSource> ApplyExcludedIds<TSource>(
        this IQueryable<TSource> queryable,
        IReadOnlyList<Guid>? excludedIds)
        where TSource : class, IEntity
    {
        if (excludedIds is not { Count: > 0 })
        {
            return queryable;
        }

        return queryable.Where(x => !excludedIds.Contains(x.Id));
    }

    public static IEnumerable<T> ApplyExcludedIds<T>(
        this IEnumerable<T> source,
        IReadOnlyList<Guid>? excludedIds,
        Func<T, Guid> getId)
    {
        if (excludedIds is not { Count: > 0 })
        {
            return source;
        }

        var excludedSet = excludedIds as HashSet<Guid> ?? excludedIds.ToHashSet();

        return source.Where(x => !excludedSet.Contains(getId(x)));
    }
}