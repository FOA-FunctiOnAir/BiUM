using MemoryPack;
using System;

namespace BiUM.Contract.Models;

[MemoryPackable]
public partial class IdNameMessage
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}