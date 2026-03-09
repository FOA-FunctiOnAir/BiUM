using System.Collections.Generic;

namespace BiUM.Contract.Models.MessageBroker;

public interface IBaseQuery
{
    public string? Q { get; init; }
    public Dictionary<string, string>? Filters { get; init; }
    public string? SortBy { get; init; }
    public SortDirection? SortDirection { get; init; }
    public int? PageStart { get; init; }
    public int? PageSize { get; init; }
}

public enum SortDirection
{
    Asc,
    Desc
}