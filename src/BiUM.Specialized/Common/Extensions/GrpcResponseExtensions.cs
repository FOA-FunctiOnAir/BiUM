using BiUM.Contract;
using BiUM.Core.Common.API;
using BiUM.Core.Common.Enums;

namespace System;

public static partial class Extensions
{
    public static void AddMessage(this GrpcResponseMeta meta, IResponseMessage message)
    {
        AddMessage(meta, message.ToArray());
    }

    public static void AddMessage(this GrpcResponseMeta meta, IReadOnlyList<IResponseMessage> messages)
    {
        meta.Success = messages.Any(m => m.Severity != MessageSeverity.Error) && meta.Success;

        foreach (var message in messages)
        {
            meta.Messages.Add(new GrpcResponseMessage()
            {
                Code = message.Code,
                Message = message.Message,
                Exception = message.Exception,
                Severity = (int)message.Severity
            });
        }
    }
}