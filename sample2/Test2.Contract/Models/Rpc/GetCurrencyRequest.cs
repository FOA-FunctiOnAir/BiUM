using MemoryPack;
using System;

namespace BiApp.Test2.Contract.Models.Rpc;

[MemoryPackable]
public sealed partial class GetCurrencyRequest
{
    public Guid CurrencyId { get; set; }
}