using MemoryPack;

namespace BiApp.Test2.Contract.Models.Rpc;

[MemoryPackable]
public sealed partial class CurrencyItem
{
    public string CurrencyId { get; set; }
    public string CurrencyCode { get; set; }
    public string CurrencyType { get; set; }
    public string CurrencyName { get; set; }
}
