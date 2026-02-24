using MemoryPack;
using System;

namespace BiApp.Test.Contract.Models.Rpc;

[MemoryPackable]
public sealed partial class GetCurrencyRequest
{
    public Guid CurrencyId { get; set; }
}