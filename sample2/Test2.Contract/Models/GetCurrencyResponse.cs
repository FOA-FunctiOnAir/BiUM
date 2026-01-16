using BiUM.Contract.Models.Api;
using MemoryPack;

namespace BiUM.Test.Contract.Models;

[MemoryPackable]
public sealed partial class GetCurrencyResponse
{
    public ResponseMeta Meta { get; set; }
    public GetCurrencyItem? Currency { get; set; }
}
