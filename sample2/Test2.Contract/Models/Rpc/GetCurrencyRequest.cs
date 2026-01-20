using MemoryPack;

namespace BiApp.Test2.Contract.Models.Rpc;

[MemoryPackable]
public sealed partial class GetCurrencyRequest
{
    public string CurrencyId { get; set; } = string.Empty;
}
