using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Enums;
using Microsoft.Extensions.Hosting;

namespace System;

public static partial class Extensions
{
    public static bool IsProductionLike(this IHostEnvironment environment, BiAppOptions appOptions)
    {
        return
            environment.IsProduction() ||
            environment.IsStaging() ||
            environment.IsEnvironment(BiAppEnvironments.Sandbox) ||
            environment.IsEnvironment("QA") ||
            appOptions is { Environment: BiAppEnvironments.Production or BiAppEnvironments.Sandbox or "Staging" or "QA" };
    }
}