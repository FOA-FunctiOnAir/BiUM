using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using System.Threading;

namespace BiUM.Infrastructure.Services.Authorization;

public sealed class CorrelationContextAccessor : ICorrelationContextAccessor
{
    private static readonly AsyncLocal<CorrelationContextHolder> CorrelationContextCurrent = new();

    public CorrelationContext? CorrelationContext
    {
        get => CorrelationContextCurrent.Value?.Context;
        set
        {
            // Clear current CorrelationContext trapped in the AsyncLocals, as its done.
            CorrelationContextCurrent.Value?.Context = null;

            if (value is not null)
            {
                // Use an object indirection to hold the CorrelationContext in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                CorrelationContextCurrent.Value = new CorrelationContextHolder { Context = value };
            }
        }
    }

    private sealed class CorrelationContextHolder
    {
        public CorrelationContext? Context;
    }
}