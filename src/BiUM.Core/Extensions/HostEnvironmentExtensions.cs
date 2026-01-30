using BiUM.Core.Common.Configs;
using Microsoft.Extensions.Hosting;

namespace System;

public static partial class Extensions
{
    public static bool IsProductionLike(this IHostEnvironment environment, BiAppOptions appOptions)
    {
        return
            environment.IsProduction() ||
            environment.IsStaging() ||
            environment.IsEnvironment("Sandbox") ||
            environment.IsEnvironment("QA") ||
            appOptions is { Environment: "Production" or "Sandbox" or "Staging" or "QA" };
    }
}