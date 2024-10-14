namespace BiUM.Core.Common.Configs;

public class BiAppOptions
{
    public const string Name = "BiAppOptions";

    public required string Environment { get; set; }

    public required string Domain { get; set; }

    public required string DomainVersion { get; set; }
}