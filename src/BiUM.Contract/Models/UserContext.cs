using MemoryPack;
using System;

namespace BiUM.Contract.Models;

[MemoryPackable]
public sealed partial class UserContext
{
    public Guid Id { get; init; }
    public Guid? PersonId { get; init; }
    public Guid? WorkgroupId { get; init; }
    public Guid? RoleId { get; init; }
    public required string Identity { get; init; }
    public required string IdentityOffice { get; init; }
    public required string FullName { get; init; }
    public required string Name { get; init; }
    public required string Surname { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
    public string? WorkgroupName { get; init; }
    public string? RoleName { get; init; }
}