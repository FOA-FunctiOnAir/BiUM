using BiUM.Contract;
using BiUM.Core.Common.API;
using BiUM.Core.Common.Enums;
using BiUM.Specialized.Common.API;

namespace BiUM;

public static partial class Extensions
{
    public static void AddMessage(this IApiResponse response, GrpcResponseMeta meta)
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