using BiUM.Contract.Models.MessageBroker;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Principal;

namespace BiUM.Contract.Models.Api;

public class PaginatedApiResponse<TType> : ApiResponse<IReadOnlyList<TType>>
{
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedApiResponse()
    {
        PageNumber = 1;
        TotalPages = 1;
        TotalCount = 0;
        Value = [];
    }

    public PaginatedApiResponse(IBaseQuery baseQuery, IList<TType> items, int count)
    {
        var (pageNumber, pageSize) = GetQueryParameters(baseQuery);

        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling((double)count / pageSize);
        TotalCount = count;
        Value = new ReadOnlyCollection<TType>(items);
    }

    public PaginatedApiResponse(IList<TType> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling((double)count / pageSize);
        TotalCount = count;
        Value = new ReadOnlyCollection<TType>(items);
    }

#pragma warning disable CA1000
    public static PaginatedApiResponse<TType> Empty() => new();
#pragma warning restore CA1000

    private static (int PageStart, int PageSize) GetQueryParameters(IBaseQuery baseQuery)
    {
        if (baseQuery is null)
        {
            return (0, 10);
        }

        var pageStart = !baseQuery.PageStart.HasValue || baseQuery.PageStart.Value < 0 ? 0 : baseQuery.PageStart.Value;
        var pageSize = !baseQuery.PageSize.HasValue || baseQuery.PageSize.Value < 0 ? 10 : baseQuery.PageSize.Value;

        return (pageStart, pageSize);
    }
}