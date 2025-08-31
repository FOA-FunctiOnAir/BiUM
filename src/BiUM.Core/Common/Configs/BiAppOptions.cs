namespace BiUM.Core.Common.Configs;

public class BiAppOptions
{
    public const string Name = "BiAppOptions";

    public string Environment { get; set; }

    public int Port { get; set; }

    public string Domain { get; set; }

    public string DomainVersion { get; set; }
}