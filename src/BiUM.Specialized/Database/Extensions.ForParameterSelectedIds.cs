using AutoMapper;
using BiUM.Contract.Models.Api;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    public static async Task MergeSelectedIdsAsync<TDestination>(
        this PaginatedApiResponse<TDestination> response,
        IReadOnlyList<Guid>? selectedIds,
        Func<TDestination, Guid> getId,
        Func<IReadOnlyList<Guid>, CancellationToken, Task<List<TDestination>>> fetchMissingAsync,
        CancellationToken cancellationToken = default)
        where TDestination : class
    {
        if (selectedIds is not { Count: > 0 } || response.Value is null)
        {
            return;
        }

        var pageIds = response.Value.Select(getId).ToHashSet();
        var missingIds = selectedIds.Where(id => !pageIds.Contains(id)).ToList();

        if (missingIds.Count == 0)
        {
            return;
        }

        var extraItems = await fetchMissingAsync(missingIds, cancellationToken);

        if (extraItems.Count == 0)
        {
            return;
        }

        var merged = new List<TDestination>(extraItems);

        merged.AddRange(response.Value);

        response.Value = merged;
    }

    public static Task MergeSelectedIdsAsync<TDestination>(
        this PaginatedApiResponse<TDestination> response,
        IReadOnlyList<Guid>? selectedIds,
        Func<IReadOnlyList<Guid>, CancellationToken, Task<List<TDestination>>> fetchMissingAsync,
        CancellationToken cancellationToken = default)
        where TDestination : BaseForValuesDto<TDestination>
    {
        return response.MergeSelectedIdsAsync(selectedIds, x => x.Id, fetchMissingAsync, cancellationToken);
    }

    public static Task MergeSelectedIdsAsync<TSource, TDestination>(
        this PaginatedApiResponse<TDestination> response,
        IReadOnlyList<Guid>? selectedIds,
        IQueryable<TSource> sourceQuery,
        IMapper mapper,
        CancellationToken cancellationToken = default)
        where TSource : class, IEntity
        where TDestination : BaseForValuesDto<TDestination>
    {
        return response.MergeSelectedIdsAsync(
            selectedIds,
            (missingIds, ct) => sourceQuery
                .Where(x => missingIds.Contains(x.Id))
                .ToListAsync<TSource, TDestination>(mapper, ct),
            cancellationToken);
    }
}