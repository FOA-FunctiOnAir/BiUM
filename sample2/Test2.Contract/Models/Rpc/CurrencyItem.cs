using MemoryPack;
using System;

namespace BiApp.Test2.Contract.Models.Rpc;

[MemoryPackable]
public sealed partial class CurrencyItem
{
    public Guid CurrencyId { get; set; }
    public required string CurrencyCode { get; set; }
    public Guid CurrencyType { get; set; }
    public required string CurrencyName { get; set; }
}