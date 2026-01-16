using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using System.Collections.Generic;
using System.Linq;
using ResponseMeta = BiUM.Contract.Models.Api.ResponseMeta;

namespace System;

public static partial class Extensions
{
    public static void AddMessage(this ResponseMeta meta, ResponseMessage message)
    {
        AddMessage(meta, message.ToArray());
    }

    public static void AddMessage(this ResponseMeta meta, IReadOnlyList<ResponseMessage> messages)
    {
        meta.Success = messages.Any(m => m.Severity != MessageSeverity.Error) && meta.Success;

        foreach (var message in messages)
        {
            meta.Messages.Add(new ResponseMessage
            {
                Code = message.Code,
                Message = message.Message,
                Exception = message.Exception,
                Severity = message.Severity
            });
        }
    }
}
