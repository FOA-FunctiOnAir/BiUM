using BiUM.Contract.Models.Api;
using MemoryPack;

namespace BiApp.Test2.Contract.Models.Rpc;

[MemoryPackable]
public sealed partial class GetCurrencyResponse : ApiResponse<CurrencyItem>;
