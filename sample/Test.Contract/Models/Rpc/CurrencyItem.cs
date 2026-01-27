using MemoryPack;

namespace BiApp.Test.Contract.Models.Rpc;

[MemoryPackable]
public sealed partial class CurrencyItem
{
    public required string CurrencyId { get; set; }
    public required string CurrencyCode { get; set; }
    public required string CurrencyType { get; set; }
    public required string CurrencyName { get; set; }
}
