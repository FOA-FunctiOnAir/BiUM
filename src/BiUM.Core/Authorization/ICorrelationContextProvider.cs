using BiUM.Contract.Models;

namespace BiUM.Core.Authorization;

public interface ICorrelationContextProvider
{
    CorrelationContext? Get();
}