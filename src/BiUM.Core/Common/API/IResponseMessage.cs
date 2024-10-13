using BiUM.Core.Common.Enums;

namespace BiUM.Core.Common.API;

public interface IResponseMessage
{
    string Code { get; set; }
    string Message { get; set; }
    string? Exception { get; set; }
    MessageSeverity Severity { get; set; }
}