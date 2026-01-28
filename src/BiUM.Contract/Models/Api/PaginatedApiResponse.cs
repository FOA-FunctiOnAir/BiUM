using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
}
