using BiUM.Contract.Enums;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Contract.Models.Api;

public class ApiResponse
{
    public virtual bool Success =>
        _messages.All(s => s.Severity != MessageSeverity.Error);

    private readonly List<ResponseMessage> _messages = [];

    public IReadOnlyList<ResponseMessage> Messages => _messages;

    public void AddMessage(ResponseMessage message)
    {
        _messages.Add(message);
    }

    public void AddMessage(IReadOnlyList<ResponseMessage> messages)
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

public class ApiResponse<TType> : ApiResponse
{
    public TType? Value { get; set; }
}
