using BiUM.Contract.Models.Api;

namespace System;

public static partial class Extensions
{
    public static void AddMessage(this ApiResponse response, ResponseMeta meta)
    {
        response.AddMessage(meta.Messages);
    }

    public static void AddMessage<TType>(this ApiResponse<TType> response, ResponseMeta meta)
    {
        response.AddMessage(meta.Messages);
    }
}
