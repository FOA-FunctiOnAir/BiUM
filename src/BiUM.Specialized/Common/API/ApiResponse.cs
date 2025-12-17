using BiUM.Core.Common.API;
using BiUM.Core.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Specialized.Common.API;

public class ApiEmptyResponse : IApiResponse
{
    public virtual bool Success =>
        _messages.All(s => s.Severity != MessageSeverity.Error);

    private readonly List<IResponseMessage> _messages = [];

    public IReadOnlyList<IResponseMessage> Messages => _messages;

    public void AddMessage(IResponseMessage message)
    {
        _messages.Add(message);
    }

    public void AddMessage(IReadOnlyList<IResponseMessage> messages)
    {
        _messages.AddRange(messages);
    }

    public void AddMessage(string message, MessageSeverity? severity)
    {
        _messages.Add(new ResponseMessage
        {
            Code = string.Empty,
            Message = message,
            Severity = severity ?? MessageSeverity.Error
        });
    }

    public void AddMessage(string code, string message, MessageSeverity? severity)
    {
        _messages.Add(new ResponseMessage
        {
            Code = code,
            Message = message,
            Severity = severity ?? MessageSeverity.Error
        });
    }

    public void AddMessage(string code, string message, string exception, MessageSeverity severity)
    {
        _messages.Add(new ResponseMessage
        {
            Code = code,
            Message = message,
            Exception = exception,
            Severity = severity
        });
    }
}

public class ApiResponse<TType> : ApiEmptyResponse, IApiResponse<TType>
{
    public TType? Value { get; set; }
}

public class PaginatedApiResponse<TType> : ApiResponse<List<TType>>
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

    public static PaginatedApiResponse<TType> Empty()
    {
        return new PaginatedApiResponse<TType>();
    }

    public PaginatedApiResponse(List<TType> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling((double)count / (double)pageSize);
        TotalCount = count;
        Value = items;
    }
}