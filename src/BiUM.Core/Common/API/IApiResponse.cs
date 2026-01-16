using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using System.Collections.Generic;

namespace BiUM.Core.Common.API;

public interface IApiResponse
{
    bool Success { get; }
    IReadOnlyList<ResponseMessage> Messages { get; }

    void AddMessage(ResponseMessage message);
    void AddMessage(IReadOnlyList<ResponseMessage> messages);
    void AddMessage(string message, MessageSeverity? severity);
    void AddMessage(string code, string message, MessageSeverity? severity);
    void AddMessage(string code, string message, string exception, MessageSeverity severity);
}

public interface IApiResponse<TType> : IApiResponse
{
    TType? Value { get; set; }
}
