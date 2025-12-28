using MessagePack;
using System;

namespace BiUM.Core.Models;

[MessagePackObject]
public class UserContext
{
    [Key(0)]
    public Guid Id { get; init; }

    [Key(1)]
    public required string Identity { get; init; }

    [Key(2)]
    public required string FullName { get; init; }

    [Key(3)]
    public required string Name { get; init; }

    [Key(4)]
    public required string Surname { get; init; }

    [Key(5)]
    public bool Active { get; init; }

    [Key(6)]
    public bool Test { get; init; }
}
