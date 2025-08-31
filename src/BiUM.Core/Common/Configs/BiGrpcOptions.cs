namespace BiUM.Core.Common.Configs;

public class BiGrpcOptions
{
    public const string Name = "BiGrpcOptions";

    public bool Enable { get; set; }

    public int Port { get; set; }

    public string Protocol { get; set; }

    public Dictionary<string, string>? Domains { get; set; }

    public string GetDomain(string domainName)
    {
        if (Domains?.TryGetValue(domainName, out var domain) == true)
        {
            return domain;
        }

        throw new ArgumentNullException(domainName, "Domain url does not exist in env.");
    }
}