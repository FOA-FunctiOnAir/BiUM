using BiUM.Contract.Enums;
using MemoryPack;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Contract.Models.Api;

[MemoryPackable]
public partial class ResponseMeta
{
    [MemoryPackInclude]
    private readonly List<ResponseMessage> _messages = [];

    [MemoryPackIgnore]
    public IReadOnlyList<ResponseMessage> Messages => _messages;

    [MemoryPackIgnore]
    public bool Success
    {
        get => _messages.All(s => s.Severity != MessageSeverity.Error);
    }

    public ResponseMeta()
    {
    }

    [MemoryPackConstructor]
    private ResponseMeta(List<ResponseMessage> messages)
    {
        _messages = messages;
    }

    public void AddMessage(ResponseMessage message)
    {
        _messages.Add(message);
    }

    public void AddMessage(IList<ResponseMessage> messages)
    {
        _messages.AddRange(messages);
    }

    public void AddMessage(IReadOnlyList<ResponseMessage> messages)
    {
        _messages.AddRange(messages);
    }

    public void AddMessage(string message, MessageSeverity? severity)
    {
        _messages.Add(new()
        {
            Code = string.Empty,
            Message = message,
            Severity = severity ?? MessageSeverity.Error
        });
    }

    public void AddMessage(string code, string message, MessageSeverity? severity)
    {
        _messages.Add(new()
        {
            Code = code,
            Message = message,
            Severity = severity ?? MessageSeverity.Error
        });
    }

    public void AddMessage(string code, string message, string exception, MessageSeverity severity)
    {
        _messages.Add(new()
        {
            Code = code,
            Message = message,
            Exception = exception,
            Severity = severity
        });
    }
}
