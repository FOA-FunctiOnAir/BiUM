using BiUM.Contract;
using BiUM.Core.Common.API;
using BiUM.Core.Common.Enums;
using BiUM.Specialized.Common.API;

namespace System;

public static partial class Extensions
{
    public static void AddMessage(this IApiResponse response, GrpcResponseMeta meta)
    {
        SetMessages(response, meta);
    }

    public static void AddMessage(this ApiEmptyResponse response, GrpcResponseMeta meta)
    {
        SetMessages(response, meta);
    }

    public static void AddMessage<TType>(this ApiResponse<TType> response, GrpcResponseMeta meta)
    {
        SetMessages(response, meta);
    }

    private static void SetMessages(IApiResponse response, GrpcResponseMeta meta)
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