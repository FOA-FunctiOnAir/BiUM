using MemoryPack;

namespace BiApp.Test.Contract.Models.Rpc;

[MemoryPackable]
public sealed partial class GetCurrencyRequest
{
    public string CurrencyId { get; set; } = string.Empty;
}
