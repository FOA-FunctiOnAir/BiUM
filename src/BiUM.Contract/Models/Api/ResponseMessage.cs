using BiUM.Contract.Enums;
using MemoryPack;

namespace BiUM.Contract.Models.Api;

[MemoryPackable]
public partial class ResponseMessage
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public MessageSeverity Severity { get; set; }
}
