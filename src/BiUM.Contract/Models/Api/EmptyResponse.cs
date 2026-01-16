using MemoryPack;

namespace BiUM.Contract.Models.Api;

[MemoryPackable]
public sealed partial class EmptyResponse
{
    public RequestMeta Meta { get; set; }
}
