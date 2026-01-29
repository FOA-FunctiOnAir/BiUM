using MemoryPack;

namespace BiApp.Test.Contract.Models.Rpc;

[MemoryPackable]
public sealed partial class GetCurrencyRequest
{
    public required string CurrencyId { get; set; }
}
