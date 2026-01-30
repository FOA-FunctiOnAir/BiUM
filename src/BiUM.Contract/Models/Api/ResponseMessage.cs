using BiUM.Contract.Enums;
using MemoryPack;

namespace BiUM.Contract.Models.Api;

[MemoryPackable]
public partial class ResponseMessage
{
    public string Code { get; init; } = string.Empty;
    public required string Message { get; init; }
    public string? Exception { get; init; }
    public MessageSeverity Severity { get; init; } = MessageSeverity.Information;
}