using BiUM.Core.Common.API;
using BiUM.Core.Common.Enums;

namespace BiUM.Specialized.Common.API;

public class ResponseMessage : IResponseMessage
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public MessageSeverity Severity { get; set; }
}