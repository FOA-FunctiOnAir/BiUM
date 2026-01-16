using MemoryPack;
using System;

namespace BiUM.Contract.Models;

[MemoryPackable]
public sealed partial class UserContext
{
    public Guid Id { get; init; }
    public required string Identity { get; init; }
    public required string FullName { get; init; }
    public required string Name { get; init; }
    public required string Surname { get; init; }
    public bool Active { get; init; }
    public bool Test { get; init; }
}
