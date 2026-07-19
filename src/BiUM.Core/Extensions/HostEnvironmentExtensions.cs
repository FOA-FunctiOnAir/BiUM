using BiUM.Core.Common.Configs;
using BiUM.Core.Constants;
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

    public static bool IsSwaggerEnabled(this IHostEnvironment environment, BiAppOptions? appOptions)
    {
        if (appOptions is { Environment: BiAppEnvironments.Production or BiAppEnvironments.Sandbox or "Staging" or "QA" })
        {
            return false;
        }

        if (appOptions is { Environment: BiAppEnvironments.Development or BiAppEnvironments.PreDevelopment })
        {
            return true;
        }

        return environment.IsDevelopment();
    }

    public static bool IsSwaggerUiEnabled(this IHostEnvironment environment, BiAppOptions? appOptions)
    {
        return environment.IsSwaggerEnabled(appOptions)
            && (environment.IsDevelopment()
                || appOptions is { Environment: BiAppEnvironments.Development or BiAppEnvironments.PreDevelopment });
    }
}