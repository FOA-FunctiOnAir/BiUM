using BiUM.Contract.Models.Api;
using MemoryPack;

namespace BiUM.Test2.Contract.Models;

[MemoryPackable]
public sealed partial class GetCurrencyRequest
{
    public RequestMeta Meta { get; set; }
    public string CurrencyId { get; set; } = string.Empty;
}
