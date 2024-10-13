namespace BiUM.Core.Common.Configs;

public class HttpClientsOptions
{
    public const string Name = "HttpClientsOptions";

    public required string Environment { get; set; } = "local";
    public required string BaseUrl { get; set; }
    public Dictionary<string, string>? Domains { get; set; }

    public string GetFullUrl(string url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith("/api/")) return url;

        string domainNameStart = url[(url.IndexOf("/api/") + 5)..];
        string domainName = domainNameStart[..domainNameStart.IndexOf('/')];

        if (Domains?.TryGetValue(domainName, out var domain) == true)
        {
            return (domain ?? BaseUrl) + url;
        }

        throw new ArgumentNullException(domainName, "Domain url does not exist in env.");
    }
}