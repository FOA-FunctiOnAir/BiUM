using BiUM.Contract.Models;
using BiUM.Core.Authorization;

namespace BiUM.Tests.Helpers;

public sealed class TestCorrelationContextAccessor : ICorrelationContextAccessor
{
    public CorrelationContext? CorrelationContext { get; set; }
}