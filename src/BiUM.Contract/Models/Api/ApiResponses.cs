using BiUM.Contract.Enums;
using MemoryPack;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Contract.Models.Api;

[MemoryPackable]
public partial class ApiResponse
{
    [MemoryPackInclude]
    protected List<ResponseMessage> ResponseMessages { get; } = [];

    [MemoryPackIgnore]
    public IReadOnlyList<ResponseMessage> Messages => ResponseMessages;

    [MemoryPackIgnore]
    public virtual bool Success =>
        ResponseMessages.All(s => s.Severity != MessageSeverity.Error);

    public ApiResponse()
    {
    }

    [MemoryPackConstructor]
    protected ApiResponse(List<ResponseMessage> responseMessages)
    {
        ResponseMessages = responseMessages;
    }

    public void AddMessage(ResponseMessage message)
    {
        ResponseMessages.Add(message);
    }

    public void AddMessage(IList<ResponseMessage> messages)
    {
        ResponseMessages.AddRange(messages);
    }

    public void AddMessage(IReadOnlyList<ResponseMessage> messages)
    {
        ResponseMessages.AddRange(messages);
    }

    public void AddMessage(string message, MessageSeverity? severity)
    {
        ResponseMessages.Add(new()
        {
            Code = string.Empty,
            Message = message,
            Severity = severity ?? MessageSeverity.Error
        });
    }

    public void AddMessage(string code, string message, MessageSeverity? severity)
    {
        ResponseMessages.Add(new()
        {
            Code = code,
            Message = message,
            Severity = severity ?? MessageSeverity.Error
        });
    }

    public void AddMessage(string code, string message, string exception, MessageSeverity severity)
    {
        ResponseMessages.Add(new()
        {
            Code = code,
            Message = message,
            Exception = exception,
            Severity = severity
        });
    }
}

[MemoryPackable]
public partial class ApiResponse<TType> : ApiResponse
{
    [MemoryPackInclude]
    public TType? Value { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(TType? value)
    {
        Value = value;
    }

    [MemoryPackConstructor]
    protected ApiResponse(TType? value, List<ResponseMessage> responseMessages) : base(responseMessages)
    {
        Value = value;
    }
}
