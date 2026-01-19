using MemoryPack;

namespace BiUM.Test.Contract.Models;

[MemoryPackable]
public sealed partial class GetCurrencyRequest
{
    public string CurrencyId { get; set; } = string.Empty;
}
