using BiUM.Contract.Models;

namespace BiUM.Core.Authorization;

public interface ICorrelationContextAccessor
{
    CorrelationContext? CorrelationContext { get; set; }
}