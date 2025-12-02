using BiUM.Core.Models;

namespace BiUM.Infrastructure.Services.Authorization;

public interface ICorrelationContextProvider
{
    CorrelationContext? Get();
}