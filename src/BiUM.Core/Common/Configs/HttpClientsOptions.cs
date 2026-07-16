using System;
using System.Collections.Generic;

namespace BiUM.Core.Common.Configs;

public class HttpClientsOptions
{
    public const string Name = "HttpClientsOptions";

    public required string Environment { get; set; } = "local";
    public required string BaseUrl { get; set; }
    public Dictionary<string, string>? Domains { get; set; }

    public string GetFullUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        var segments = GetApiSegments(url);

        if (segments is null)
        {
            return url;
        }

        var baseUrl = FindDomainBaseUrl(segments);

        if (baseUrl is not null)
        {
            return $"{baseUrl}{url}";
        }

        throw new InvalidOperationException($"{segments[0]} not found in Domains");
    }

    public string GetFullUrl(string microserviceRootPath, string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        var segments = GetApiSegments(url);

        if (segments is null)
        {
            return url;
        }

        if (segments[0].Equals("base", StringComparison.InvariantCultureIgnoreCase)
            && Domains?.TryGetValue(microserviceRootPath, out var microserviceBaseUrl) is true)
        {
            return $"{microserviceBaseUrl}{url}";
        }

        var baseUrl = FindDomainBaseUrl(segments);

        if (baseUrl is not null)
        {
            return $"{baseUrl}{url}";
        }

        throw new InvalidOperationException($"{segments[0]} not found in Domains");
    }

    // Returns path segments after /api/, or null if /api/ not found.
    private static string[]? GetApiSegments(string url)
    {
        var startIndex = url.IndexOf("/api/", StringComparison.InvariantCultureIgnoreCase);

        if (startIndex == -1)
        {
            return null;
        }

        var pathAfterApi = url[(startIndex + 5)..];

        return pathAfterApi.Split('/');
    }

    // Tries longest-prefix-first: "zrt-abc" before "zrt", matching multi-segment domain codes.
    private string? FindDomainBaseUrl(string[] segments)
    {
        for (var depth = segments.Length; depth >= 1; depth--)
        {
            var candidate = string.Join('-', segments, 0, depth);

            if (Domains?.TryGetValue(candidate, out var baseUrl) is true)
            {
                return baseUrl;
            }
        }

        return null;
    }
}