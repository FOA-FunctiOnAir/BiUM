using BiUM.Core.Models;

namespace BiUM.Core.Authorization;

public interface ICorrelationContextProvider
{
    CorrelationContext? Get();
}