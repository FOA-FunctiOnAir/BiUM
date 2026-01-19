using MemoryPack;

namespace BiUM.Test2.Contract.Models;

[MemoryPackable]
public sealed partial class GetCurrencyRequest
{
    public string CurrencyId { get; set; } = string.Empty;
}
