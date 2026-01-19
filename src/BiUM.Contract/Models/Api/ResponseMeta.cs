using MemoryPack;
using System.Collections.Generic;

namespace BiUM.Contract.Models.Api;

[MemoryPackable]
public partial class ResponseMeta
{
    public bool Success { get; set; }
    public IList<ResponseMessage> Messages { get; set; } = [];
}
