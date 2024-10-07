namespace BiUM.Core.Common.Configs;

public class BoltOptions
{
    public const string Name = "BoltOptions";

    public bool Enable { get; set; }

    public string Branch { get; set; }

    public bool UseHttp { get; set; }

    public string HttpUrl { get; set; }

    public bool UseDbConnection { get; set; }

    public string ConnectionString { get; set; }
}