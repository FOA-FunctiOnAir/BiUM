using BiUM.Contract.Models;
using BiUM.Core.Authorization;

namespace BiUM.Tests.Helpers;

public sealed class TestCorrelationContextProvider : ICorrelationContextProvider
{
    public CorrelationContext Context { get; set; } = CorrelationContext.Empty;

    public CorrelationContext? Get() => Context;
}