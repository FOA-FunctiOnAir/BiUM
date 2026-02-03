using System;
using System.Reflection;

namespace BiUM.Core.Common.Utils;

public static class VersionHelper
{
    public static Version Version => field ??= GetVersion();

    private static Version GetVersion()
    {
        var assembly = typeof(VersionHelper).Assembly;

        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion) && Version.TryParse(informationalVersion, out var version))
        {
            return version;
        }

        return assembly.GetName().Version ?? new Version(0, 0, 0);
    }
}