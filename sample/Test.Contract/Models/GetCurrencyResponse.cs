using BiUM.Contract.Models.Api;
using MemoryPack;

namespace BiUM.Test.Contract.Models;

[MemoryPackable]
public sealed partial class GetCurrencyResponse : ApiResponse<GetCurrencyItem>;
