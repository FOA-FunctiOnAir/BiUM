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
    public virtual bool Success => ResponseMessages.All(s => s.Severity != MessageSeverity.Error);

    [JsonIgnore]
    [MemoryPackIgnore]
    public virtual bool Warning => ResponseMessages.All(s => s.Severity == MessageSeverity.Warning);

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
        ResponseMessages = messages.ToList();
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