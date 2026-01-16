using MemoryPack;

namespace BiUM.Contract.Models;

[MemoryPackable]
public partial class IdNameMessage
{
    public string Id { get; set; }
    public string Name { get; set; }
}
