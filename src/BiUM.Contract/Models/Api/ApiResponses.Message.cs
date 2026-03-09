using System.Collections.Generic;

namespace BiUM.Contract.Models.Api;

public partial class ApiResponse
{
    public void AddMessage(ResponseMessage message)
    {
        if (message is null)
        {
            return;
        }

        ResponseMessages.Add(message);
    }

    public void AddMessage(IList<ResponseMessage> messages)
    {
        ResponseMessages.AddRange(messages ?? []);
    }

    public void AddMessage(IReadOnlyList<ResponseMessage> messages)
    {
        ResponseMessages.AddRange(messages ?? []);
    }

    public void AddMessage(ApiResponse response)
    {
        ResponseMessages.AddRange(response?.ResponseMessages ?? []);
    }
}