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

        var startIndex = url.IndexOf("/api/", StringComparison.InvariantCultureIgnoreCase);

        if (startIndex == -1)
        {
            return url;
        }

        var endIndex = url.IndexOf('/', startIndex + 5);

        if (endIndex == -1)
        {
            return url;
        }

        var serviceKey = url[(startIndex + 5)..endIndex];

        if (Domains?.TryGetValue(serviceKey, out var baseUrl) is true)
        {
            return $"{baseUrl}{url}";
        }

        throw new InvalidOperationException($"{serviceKey} not found in Domains");
    }
}