using System;

namespace BiUM.Specialized.Common.Utils;

public static class MicroserviceCodeHelper
{
    public static string FromRootPath(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return string.Empty;
        }

        var normalized = rootPath.Trim().TrimEnd('/');

        if (normalized.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["/api/".Length..];
        }
        else if (normalized.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["api/".Length..];
        }

        return normalized.Replace('/', '-').ToLowerInvariant();
    }
}