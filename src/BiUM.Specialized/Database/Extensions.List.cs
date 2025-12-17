using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var items = mapper.Map<List<TDestination>>(await query.ToListAsync(cancellationToken));

        return items;
    }

    public static async Task<IList<TDestination>> ToIListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        return await queryable.ToListAsync<TSource, TDestination>(mapper, cancellationToken);
    }
}