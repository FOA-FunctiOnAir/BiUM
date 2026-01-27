namespace BiUM.Core.Common.Configs;

public class BoltOptions
{
    public const string Name = "BoltOptions";

    public bool Enable { get; set; }

    public required string Branch { get; set; }

    public required string Server { get; set; }

    public required string ConnectionString { get; set; }
}
