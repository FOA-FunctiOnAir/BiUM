using MemoryPack;

namespace BiUM.Test2.Contract.Models;

[MemoryPackable]
public sealed partial class GetCurrencyItem
{
    public string CurrencyId { get; set; }
    public string CurrencyCode { get; set; }
    public string CurrencyType { get; set; }
    public string CurrencyName { get; set; }
}
