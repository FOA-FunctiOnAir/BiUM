using BiUM.Contract.Models.Api;
using MemoryPack;
using System.Collections.Generic;

namespace BiApp.Test2.Contract.Models.Rpc;

[MemoryPackable]
public sealed partial class GetCurrencyResponse : ApiResponse<CurrencyItem>
{
    public GetCurrencyResponse()
    {
    }

    [MemoryPackConstructor]
    private GetCurrencyResponse(CurrencyItem? value, List<ResponseMessage> responseMessages) : base(value, responseMessages)
    {
    }
}
