using System.Reflection;

namespace BiUM.Core.Common.Utils;

public static class VersionHelper
{
    public static string Version => field ??= GetVersion();

    private static string GetVersion()
    {
        var assembly = typeof(VersionHelper).Assembly;

        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}