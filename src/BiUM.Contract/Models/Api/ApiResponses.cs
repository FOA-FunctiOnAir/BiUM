using BiUM.Contract.Enums;
using MemoryPack;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BiUM.Contract.Models.Api;

[MemoryPackable]
public partial class ApiResponse
{
    [JsonIgnore]
    [MemoryPackInclude]
    protected List<ResponseMessage> ResponseMessages { get; } = [];

    [JsonInclude]
    [MemoryPackIgnore]
    public IReadOnlyList<ResponseMessage> Messages => ResponseMessages;

    [JsonInclude]
    [MemoryPackIgnore]
    public virtual bool Success =>
        ResponseMessages.All(s => s.Severity != MessageSeverity.Error);

    public ApiResponse()
    {
    }

#pragma warning disable CA1000
    public static ApiResponse<IList<TValue>> EmptyArray<TValue>() => new() { Value = [] };
#pragma warning restore CA1000

    [MemoryPackConstructor]
    protected ApiResponse(List<ResponseMessage> responseMessages)
    {
        ResponseMessages = responseMessages;
    }

    [JsonConstructor]
    protected ApiResponse(IReadOnlyList<ResponseMessage> messages, bool success)
    {
        AddMessage(messages);
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

    public void AddMessage(ApiResponse response)
    {
        ResponseMessages.AddRange(response.ResponseMessages);
    }

    public void AddMessage(string message)
    {
        ResponseMessages.Add(new()
        {
            Code = "unknown_error",
            Message = message,
            Severity = MessageSeverity.Error
        });
    }

    public void AddMessage(string message, MessageSeverity severity)
    {
        ResponseMessages.Add(new()
        {
            Code =
                severity switch
                {
                    MessageSeverity.Warning => "unknown_warning",
                    MessageSeverity.Error => "unknown_error",
                    _ => severity.ToString()
                },
            Message = message,
            Severity = severity
        });
    }

    public void AddMessage(string code, string message, MessageSeverity severity)
    {
        ResponseMessages.Add(new()
        {
            Code = code,
            Message = message,
            Severity = severity
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
public partial class ApiResponse<TValue> : ApiResponse
{
    [JsonInclude]
    [MemoryPackInclude]
    public TValue? Value { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(TValue? value)
    {
        Value = value;
    }

    //#pragma warning disable CA1000
    //    //public static ApiResponse<IList<TValue>> EmptyArray() => new() { Value = [] };
    //    public static ApiResponse<IList<TValue>> EmptyArray<TValue>() => new() { Value = [] };
    //#pragma warning restore CA1000

    [MemoryPackConstructor]
    protected ApiResponse(TValue? value, List<ResponseMessage> responseMessages) : base(responseMessages)
    {
        Value = value;
    }

    [JsonConstructor]
    protected ApiResponse(TValue? value, IReadOnlyList<ResponseMessage> messages, bool success) : base(messages, success)
    {
        Value = value;
    }
}