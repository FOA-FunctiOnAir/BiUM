using BiUM.Core.Common.Enums;
using System.Collections.Generic;

namespace BiUM.Core.Common.API;

public interface IApiResponse
{
    bool Success { get; }
    IReadOnlyList<IResponseMessage> Messages { get; }

    void AddMessage(IResponseMessage message);
    void AddMessage(IReadOnlyList<IResponseMessage> messages);
    void AddMessage(string message, MessageSeverity? severity);
    void AddMessage(string code, string message, MessageSeverity? severity);
    void AddMessage(string code, string message, string exception, MessageSeverity severity);
}

public interface IApiResponse<TType> : IApiResponse
{
    TType? Value { get; set; }
}
