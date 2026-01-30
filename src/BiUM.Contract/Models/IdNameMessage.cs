using MemoryPack;

namespace BiUM.Contract.Models;

[MemoryPackable]
public partial class IdNameMessage
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}