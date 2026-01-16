using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Core.Common.API;
using BiUM.Specialized.Common.API;

namespace System;

public static partial class Extensions
{
    public static void AddMessage(this IApiResponse response, ResponseMeta meta)
    {
        SetMessages(response, meta);
    }

    public static void AddMessage(this ApiEmptyResponse response, ResponseMeta meta)
    {
        SetMessages(response, meta);
    }

    public static void AddMessage<TType>(this ApiResponse<TType> response, ResponseMeta meta)
    {
        SetMessages(response, meta);
    }

    private static void SetMessages(IApiResponse response, ResponseMeta meta)
    {
        var messages = meta?.Messages;

        if (messages?.Count > 0)
        {
            foreach (var message in messages)
            {
                response.AddMessage(new ResponseMessage
                {
                    Code = message.Code,
                    Message = message.Message,
                    Exception = message.Exception,
                    Severity = (MessageSeverity)message.Severity
                });
            }
        }
    }
}
