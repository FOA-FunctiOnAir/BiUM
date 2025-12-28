namespace BiUM.Core.Common.Configs;

public class BoltOptions
{
    public const string Name = "BoltOptions";

    public bool Enable { get; set; }

    public string Branch { get; set; }

    public string Server { get; set; }

    public string ConnectionString { get; set; }
}
