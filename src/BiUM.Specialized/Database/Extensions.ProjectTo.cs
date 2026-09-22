using AutoMapper;
using AutoMapper.QueryableExtensions;
using BiUM.Contract.Models.Api;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Mapper;
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
    public static Task<List<TDestination>> ProjectToListAsync<TDestination>(
        this IQueryable source,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TDestination : class
        => source.ProjectTo<TDestination>(mapper.ConfigurationProvider).AsNoTracking().ToListAsync(cancellationToken);

    public static Task<TDestination?> ProjectToFirstOrDefaultAsync<TDestination>(
        this IQueryable source,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TDestination : class
        => source.ProjectTo<TDestination>(mapper.ConfigurationProvider).AsNoTracking().FirstOrDefaultAsync(cancellationToken);

    public static Task<TDestination?> ProjectToFirstOrDefaultAsync<TDestination>(
        this IQueryable source,
        Expression<Func<TDestination, bool>> predicate,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TDestination : class
        => source.ProjectTo<TDestination>(mapper.ConfigurationProvider).AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

    public static Task ProjectToMergeSelectedIdsAsync<TSource, TDestination>(
        this PaginatedApiResponse<TDestination> response,
        IReadOnlyList<Guid>? selectedIds,
        IQueryable<TSource> sourceQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
    {
        return response.MergeSelectedIdsAsync(
            selectedIds,
            (missingIds, ct) => sourceQuery
                .Where(x => missingIds.Contains(x.Id))
                .ToListAsync<TSource, TDestination>(mapper, ct),
            cancellationToken);
    }

    public static Task ProjectToMergeSelectedIdsAsync<TSource, TDestination>(
        this PaginatedApiResponse<TDestination> response,
        IReadOnlyList<Guid>? selectedIds,
        IQueryable<TSource> sourceQuery,
        IMapper mapper,
        Guid languageId,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : ForValuesDtoBase
    {
        return response.MergeSelectedIdsAsync(
            selectedIds,
            (missingIds, ct) => sourceQuery
                .Where(x => missingIds.Contains(x.Id))
                .ToListAsync<TSource, TDestination>(mapper, languageId, ct),
            cancellationToken);
    }
}